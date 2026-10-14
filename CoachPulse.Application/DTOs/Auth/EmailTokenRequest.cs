namespace CoachPulse.Application.DTOs.Auth;

public sealed class EmailTokenRequest : TenantEmailRequest
{
    public string Token { get; set; } = string.Empty;
}
