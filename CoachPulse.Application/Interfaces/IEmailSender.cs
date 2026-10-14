namespace CoachPulse.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}
