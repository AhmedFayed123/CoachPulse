using CoachPulse.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace CoachPulse.Infrastructure.Services;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger, IWebHostEnvironment environment) : IEmailDeliveryTransport
{
    public string ProviderName => "Dev";

    public Task SendAsync(string email, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        if (string.Equals(environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            logger.LogInformation("Development email queued for {RecipientDomain}; template subject: {Subject}. Message content is omitted.", email.Split('@').LastOrDefault() ?? "unknown", subject);
        else
            logger.LogWarning("Dev email provider selected outside Development; delivery skipped. Subject: {Subject}.", subject);
        return Task.CompletedTask;
    }
}
