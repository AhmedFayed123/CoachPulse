namespace CoachPulse.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
}
