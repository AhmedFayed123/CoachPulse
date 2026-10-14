using CoachPulse.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace CoachPulse.Infrastructure.Services;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger, IWebHostEnvironment environment) : IEmailSender
{
    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.Equals(environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            logger.LogInformation("Development email to {Email}: {Subject}. Body: {Body}", email, subject, body);
        else
            logger.LogWarning("Email delivery is not configured for {Email} ({Subject}); message body was suppressed.", email, subject);
        return Task.CompletedTask;
    }
}
