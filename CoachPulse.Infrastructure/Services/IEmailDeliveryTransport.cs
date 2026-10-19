namespace CoachPulse.Infrastructure.Services;

public interface IEmailDeliveryTransport
{
    string ProviderName { get; }
    Task SendAsync(string email, string subject, string htmlBody, string textBody, CancellationToken cancellationToken);
}
