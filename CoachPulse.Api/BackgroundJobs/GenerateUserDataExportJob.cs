using System.Security.Cryptography;
using System.Text.Json;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.BackgroundJobs;

public sealed class GenerateUserDataExportJob(CoachPulseDbContext db, IFileStorage storage, ILogger<GenerateUserDataExportJob> logger)
{
    private const int MaxBytes = 10 * 1024 * 1024;

    public async Task RunAsync(Guid requestId, CancellationToken ct)
    {
        var request = await db.DataExportRequests.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == requestId, ct);
        if (request is null || request.Status != "Pending") return;
        try
        {
            var tenantId = request.TenantId;
            var user = await db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId && x.TenantId == tenantId && !x.IsDeleted, ct);
            if (user is null) { request.Status = "Failed"; request.FailureCode = "account_unavailable"; await db.SaveChangesAsync(ct); return; }
            var client = tenantId is null ? null : await db.Clients.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id && x.TenantId == tenantId, ct);
            var checkIns = client is null ? new List<object>() : (await db.CheckIns.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.ClientId == client.Id && !x.IsDeleted).Select(x => new { x.Date, x.Weight, x.Measurements, x.CoachFeedback }).ToListAsync(ct)).Cast<object>().ToList();
            var workouts = client is null ? new List<object>() : (await db.WorkoutLogs.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.ClientId == client.Id && !x.IsDeleted).Select(x => new { x.PerformedOn, x.ProgramExerciseId, x.SetNumber, x.Reps, x.WeightKg }).ToListAsync(ct)).Cast<object>().ToList();
            var payments = client is null ? new List<object>() : (await db.Payments.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.Subscription.ClientId == client.Id).Select(x => new { x.CreatedAt, x.Amount, x.Currency, x.Method, x.Status, x.InvoiceNumber }).ToListAsync(ct)).Cast<object>().ToList();
            var messages = tenantId is null ? new List<object>() : (await db.Messages.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && (x.SenderId == user.Id || x.ReceiverId == user.Id)).Select(x => new { x.Timestamp, x.SenderId, x.ReceiverId, x.Content, x.IsDeleted }).ToListAsync(ct)).Cast<object>().ToList();
            var subscriptions = client is null ? new List<object>() : (await db.Subscriptions.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.ClientId == client.Id).Select(x => new { x.Status, x.StartDate, x.EndDate, x.SnapshotPrice, x.SnapshotDurationDays }).ToListAsync(ct)).Cast<object>().ToList();
            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                exportedAtUtc = DateTime.UtcNow,
                profile = new { user.Name, user.Email, user.Phone, user.Language },
                healthInfo = client?.HealthInfo,
                checkIns,
                workoutLogs = workouts,
                payments,
                subscriptions,
                messages
            });
            if (payload.Length > MaxBytes) { request.Status = "Failed"; request.FailureCode = "export_size_limit"; await db.SaveChangesAsync(ct); return; }
            var storagePath = await storage.SaveAsync(new MemoryStream(payload), user.Id.ToString("N"), ".json", ct);
            if (tenantId is Guid scopedTenant)
            {
                var file = new StoredFile { Id = Guid.NewGuid(), TenantId = scopedTenant, OwnerUserId = user.Id, Purpose = "data-export", OriginalName = "personal-data-export.json", ContentType = "application/json", SizeBytes = payload.Length, StoragePath = storagePath, Sha256 = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), CreatedAt = DateTime.UtcNow, LinkedEntityType = "DataExport", LinkedEntityId = request.Id };
                db.StoredFiles.Add(file); request.StoredFileId = file.Id;
            }
            request.StoragePath = storagePath; request.SizeBytes = payload.Length; request.Status = "Ready"; request.ExpiresAt = DateTime.UtcNow.AddHours(1);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Personal data export failed for request {ExportRequestId}.", requestId);
            db.ChangeTracker.Clear();
            var failed = await db.DataExportRequests.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == requestId, ct);
            if (failed is not null) { failed.Status = "Failed"; failed.FailureCode = "generation_failed"; await db.SaveChangesAsync(ct); }
        }
    }

    public async Task CleanupAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expired = await db.DataExportRequests.IgnoreQueryFilters().Where(x =>
            (x.Status == "Ready" && (x.ExpiresAt <= now || x.DownloadedAt != null)) ||
            (x.Status == "Failed" && x.CreatedAt < now.AddDays(-7))).Take(500).ToListAsync(ct);
        foreach (var export in expired)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(export.StoragePath)) await storage.DeleteAsync(export.StoragePath, ct);
                if (export.StoredFileId is Guid fileId && export.TenantId is Guid tenantId)
                {
                    var file = await db.StoredFiles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == fileId && x.TenantId == tenantId, ct);
                    if (file is not null) file.IsDeleted = true;
                }
                export.Status = "Expired"; export.StoragePath = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Expired data export cleanup failed for request {ExportRequestId}.", export.Id);
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
