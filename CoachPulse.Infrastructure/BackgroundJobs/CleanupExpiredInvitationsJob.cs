using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Infrastructure.BackgroundJobs;

public sealed class CleanupExpiredInvitationsJob(CoachPulseDbContext db, ILogger<CleanupExpiredInvitationsJob> logger)
{
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var tenants = await db.Tenants.IgnoreQueryFilters().AsNoTracking().Where(x => x.Status == "Active" && !x.IsDeleted).Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var tenantId in tenants)
        {
            try
            {
                var expired = await db.ClientInvitations.IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.AcceptedAt == null && x.ExpiresAt < cutoff)
                    .ToListAsync(cancellationToken);
                db.ClientInvitations.RemoveRange(expired);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Removed {Count} expired pending invitations for tenant {TenantId}.", expired.Count, tenantId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Expired invitation cleanup failed for tenant {TenantId}.", tenantId);
            }
        }
    }
}
