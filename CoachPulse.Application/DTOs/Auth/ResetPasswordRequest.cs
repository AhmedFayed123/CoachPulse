namespace CoachPulse.Application.DTOs.Auth;

public sealed class ResetPasswordRequest : TenantEmailRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
