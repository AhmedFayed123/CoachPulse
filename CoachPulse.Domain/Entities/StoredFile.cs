using CoachPulse.Domain.Interfaces;

namespace CoachPulse.Domain.Entities;

public sealed class StoredFile : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
    public bool IsDeleted { get; set; }
}
