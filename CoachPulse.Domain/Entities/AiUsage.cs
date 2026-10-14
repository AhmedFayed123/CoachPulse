using CoachPulse.Domain.Interfaces;

namespace CoachPulse.Domain.Entities;

public sealed class AiUsage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid GeneratedProgramId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
