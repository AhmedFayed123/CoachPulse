namespace CoachPulse.Domain.Entities;

public sealed class IdempotencyKey
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
