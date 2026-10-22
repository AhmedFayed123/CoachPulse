namespace CoachPulse.Application.DTOs.Auth;

public class TenantEmailRequest
{
    public string? TenantSlug { get; set; }
    public string Email { get; set; } = string.Empty;
}
