namespace CoachPulse.Application.DTOs.Clients;

public sealed class UpdateCheckInScheduleRequest
{
    public string Frequency { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
}
