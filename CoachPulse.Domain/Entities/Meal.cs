namespace CoachPulse.Domain.Entities;

public sealed class Meal
{
    public Guid Id { get; set; }
    public Guid MealPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly? Time { get; set; }
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
}
