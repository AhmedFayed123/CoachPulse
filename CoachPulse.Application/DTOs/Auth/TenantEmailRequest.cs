namespace CoachPulse.Application.DTOs.Auth;

public class TenantEmailRequest
{
    public string TenantSlug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
