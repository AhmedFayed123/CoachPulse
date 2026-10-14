using CoachPulse.Domain.Interfaces;

namespace CoachPulse.Domain.Entities;

public sealed class WorkoutLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ProgramExerciseId { get; set; }
    public DateOnly PerformedOn { get; set; }
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime PerformedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
