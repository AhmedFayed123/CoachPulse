using System.Text.Json;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Api.Hubs;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Services;

public sealed class ChurnScoringService(CoachPulseDbContext db, IAiClient ai, IChurnFeatureBuilder featureBuilder, IHubContext<NotificationsHub> hub, ILogger<ChurnScoringService> logger, INotificationService? notifications = null)
{
    public async Task<int> ScoreTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId && x.Status == "Active", ct);
        if (tenant is null) return 0;
        var clients = await db.Clients.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.Status == ClientStatus.Active).AsNoTracking().ToListAsync(ct);
        var saved = 0;
        foreach (var batch in clients.Chunk(100))
        {
            try
            {
                var requests = new List<AiChurnRequest>();
                foreach (var c in batch)
                {
                    requests.Add(new AiChurnRequest(c.Id, await featureBuilder.BuildAsync(tenantId, c, ct)));
                }
                var predictions = await ai.PredictChurnBatchAsync(requests, ct);
                foreach (var result in predictions.Items)
                {
                    var today = DateTime.UtcNow.Date;
                    var existingToday = await db.ChurnRiskLogs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientId == result.ClientId && x.ComputedAt >= today, ct);
                    var previous = await db.ChurnRiskLogs.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.ClientId == result.ClientId && x.ComputedAt < today).OrderByDescending(x => x.ComputedAt).Select(x => x.RiskLevel).FirstOrDefaultAsync(ct);
                    if (existingToday is null) db.ChurnRiskLogs.Add(new ChurnRiskLog { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = result.ClientId, RiskScore = (decimal)result.RiskScore, RiskLevel = result.RiskLevel, TopFactorsJson = JsonSerializer.Serialize(result.TopFactors), ModelVersion = result.ModelVersion, ComputedAt = DateTime.UtcNow });
                    else { existingToday.RiskScore = (decimal)result.RiskScore; existingToday.RiskLevel = result.RiskLevel; existingToday.TopFactorsJson = JsonSerializer.Serialize(result.TopFactors); existingToday.ModelVersion = result.ModelVersion; existingToday.ComputedAt = DateTime.UtcNow; }
                    if (existingToday is null && result.RiskLevel == "High" && previous is not "High")
                    {
                        var coachId = batch.FirstOrDefault(x => x.Id == result.ClientId)?.CoachId;
                        if (coachId.HasValue)
                        {
                            var payload = new { clientId = result.ClientId, riskScore = result.RiskScore, riskLevel = result.RiskLevel };
                            if (notifications is not null) await notifications.NotifyAsync(coachId.Value, tenantId, "ChurnRiskAlert", "تنبيه احتمالية مغادرة عميل", "تم رصد ارتفاع احتمالية مغادرة أحد العملاء.", JsonSerializer.Serialize(payload), ct);
                            else await hub.Clients.User(coachId.Value.ToString()).SendAsync("ChurnRiskAlert", payload, ct);
                        }
                    }
                    saved++;
                }
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Churn scoring failed for tenant {TenantId} batch; continuing.", tenantId); db.ChangeTracker.Clear(); }
        }
        return saved;
    }
    public async Task<int> ScoreActiveTenantsAsync(CancellationToken ct) { var ids = await db.Tenants.IgnoreQueryFilters().Where(x => x.Status == "Active").Select(x => x.Id).ToListAsync(ct); var total = 0; foreach (var id in ids) total += await ScoreTenantAsync(id, ct); return total; }
}
