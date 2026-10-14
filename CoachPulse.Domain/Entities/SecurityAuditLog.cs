namespace CoachPulse.Domain.Entities;

public sealed class SecurityAuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
