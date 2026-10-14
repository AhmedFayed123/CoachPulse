namespace CoachPulse.Domain.Entities;

public sealed class DataExportRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? StoredFileId { get; set; }
    public string? StoragePath { get; set; }
    public long? SizeBytes { get; set; }
    public string DownloadTokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DownloadedAt { get; set; }
    public string? FailureCode { get; set; }
}
