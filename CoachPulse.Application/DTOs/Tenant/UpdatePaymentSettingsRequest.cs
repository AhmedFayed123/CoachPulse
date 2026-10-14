namespace CoachPulse.Application.DTOs.Tenant;

public sealed class UpdatePaymentSettingsRequest
{
    public bool PaymobEnabled { get; set; }
    public bool InstaPayEnabled { get; set; }
    public string? InstaPayId { get; set; }
    public string? InstaPayAccountNumber { get; set; }
}
