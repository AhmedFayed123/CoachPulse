using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Services;

public sealed class ChurnFeatureBuilder(CoachPulseDbContext db) : IChurnFeatureBuilder
{
    public async Task<AiChurnFeatures> BuildAsync(Guid tenantId, Client client, CancellationToken ct)
    {
        var since14 = DateTime.UtcNow.AddDays(-14);
        var since30 = DateTime.UtcNow.AddDays(-30);
        var checkins = await db.CheckIns.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.ClientId == client.Id && !x.IsDeleted && x.Date >= since14, ct);
        var checkins30 = await db.CheckIns.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.ClientId == client.Id && !x.IsDeleted && x.Date >= since30, ct);
        var messages = await db.Messages.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.SenderId == client.UserId && x.Timestamp >= since14 && !x.IsDeleted, ct);
        var lastSubscription = await db.Subscriptions.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.ClientId == client.Id && x.Status == SubscriptionStatus.Active).OrderByDescending(x => x.EndDate).Select(x => (DateTime?)x.EndDate).FirstOrDefaultAsync(ct);
        var renewals = await db.Subscriptions.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.ClientId == client.Id && (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Expired), ct);
        var pending = await db.Subscriptions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.ClientId == client.Id && x.Status == SubscriptionStatus.Pending, ct);
        // Explicit missing-telemetry defaults: reply latency=72h, last login=30d. User login telemetry is not modeled.
        var expectedCheckins = client.CheckInFrequency switch { CheckInFrequency.Weekly => 4d, CheckInFrequency.BiWeekly => 2d, CheckInFrequency.Monthly => 1d, _ => 4d };
        var workout = await WorkoutAdherenceCalculator.CalculateAsync(db, tenantId, client.Id, DateTime.UtcNow.AddDays(-14), DateTime.UtcNow, ct);
        var workoutRate = workout.Rate.HasValue ? (double)workout.Rate.Value : 0d; // no active plan/logs => documented default 0
        return new AiChurnFeatures(checkins, Math.Min(1, checkins30 / expectedCheckins), workoutRate, 72d, 30,
            lastSubscription is null ? 365 : (int)(lastSubscription.Value.Date - DateTime.UtcNow.Date).TotalDays,
            messages, Math.Max(0, renewals - 1), pending);
    }
}
