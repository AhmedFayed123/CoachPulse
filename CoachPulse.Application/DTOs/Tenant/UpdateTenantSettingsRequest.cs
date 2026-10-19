namespace CoachPulse.Application.DTOs.Tenant;

public sealed class UpdateTenantSettingsRequest
{
    public string Currency { get; set; } = "EGP";
    public string TimeZone { get; set; } = "Africa/Cairo";
    public int ReminderDays { get; set; } = 7;
    public string DefaultLanguage { get; set; } = "en";
}
