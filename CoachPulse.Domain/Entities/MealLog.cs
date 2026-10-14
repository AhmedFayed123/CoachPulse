using CoachPulse.Domain.Interfaces;

namespace CoachPulse.Domain.Entities;

public sealed class MealLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid MealId { get; set; }
    public DateOnly LoggedOn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
