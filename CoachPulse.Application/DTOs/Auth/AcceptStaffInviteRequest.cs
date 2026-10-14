namespace CoachPulse.Application.DTOs.Auth;

public sealed class AcceptStaffInviteRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
