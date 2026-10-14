namespace CoachPulse.Application.DTOs.Account;

public sealed class UpdateAccountRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public Guid? AvatarFileId { get; set; }
    public string? Language { get; set; }
}
