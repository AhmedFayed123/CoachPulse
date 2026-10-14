using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Infrastructure.BackgroundJobs;

public sealed class CleanupReadNotificationsJob(CoachPulseDbContext db, ILogger<CleanupReadNotificationsJob> logger)
{
    public async Task CleanupAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var tenants = await db.Tenants.IgnoreQueryFilters().AsNoTracking().Where(t => t.Status == "Active" && !t.IsDeleted).Select(t => t.Id).ToListAsync(ct);
        foreach (var tenantId in tenants)
        {
            try
            {
                var count = await db.Notifications.IgnoreQueryFilters().Where(n => n.TenantId == tenantId && n.IsRead && n.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
                logger.LogInformation("Removed {Count} read notifications older than 90 days for tenant {TenantId}.", count, tenantId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Read notification cleanup failed for tenant {TenantId}.", tenantId);
            }
        }
    }
}
