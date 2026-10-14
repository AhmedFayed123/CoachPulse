using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Infrastructure.BackgroundJobs;

public sealed class CleanupOrphanFilesJob
{
    private readonly CoachPulseDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ILogger<CleanupOrphanFilesJob> _logger;

    public CleanupOrphanFilesJob(CoachPulseDbContext db, IFileStorage storage, ILogger<CleanupOrphanFilesJob> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        var orphanCutoff = DateTime.UtcNow.AddDays(-7);
        var tenants = await _db.Tenants.IgnoreQueryFilters().AsNoTracking().Where(t => t.Status == "Active" && !t.IsDeleted).Select(t => t.Id).ToListAsync(cancellationToken);
        var processed = 0;
        foreach (var tenantId in tenants)
        {
            var orphanFiles = await _db.StoredFiles.IgnoreQueryFilters()
                .Where(file => file.TenantId == tenantId && !file.IsDeleted && file.LinkedEntityId == null && file.LinkedEntityType == null && file.CreatedAt < orphanCutoff)
                .ToListAsync(cancellationToken);
            foreach (var file in orphanFiles)
            {
                try
                {
                    await _storage.DeleteAsync(file.StoragePath, cancellationToken);
                    file.IsDeleted = true;
                    processed++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(exception, "Failed to remove orphaned file {FileId} from tenant {TenantId} storage.", file.Id, tenantId);
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        _logger.LogInformation("Cleaned up {Count} orphaned files older than seven days across active tenants.", processed);
    }
}
