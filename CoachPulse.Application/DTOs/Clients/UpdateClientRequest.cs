namespace CoachPulse.Application.DTOs.Clients;

public sealed class UpdateClientRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Goals { get; set; }
}
