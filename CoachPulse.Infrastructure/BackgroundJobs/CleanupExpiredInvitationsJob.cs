using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Infrastructure.BackgroundJobs;

public sealed class CleanupExpiredInvitationsJob(CoachPulseDbContext db, ILogger<CleanupExpiredInvitationsJob> logger)
{
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var expired = await db.ClientInvitations.IgnoreQueryFilters()
            .Where(x => x.AcceptedAt == null && x.ExpiresAt < cutoff)
            .ToListAsync(cancellationToken);
        db.ClientInvitations.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Removed {Count} expired client invitations.", expired.Count);
    }
}
