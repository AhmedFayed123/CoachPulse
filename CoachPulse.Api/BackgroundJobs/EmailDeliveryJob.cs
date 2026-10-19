using CoachPulse.Infrastructure.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Api.BackgroundJobs;

public sealed class EmailDeliveryJob(IEmailDeliveryTransport transport, ILogger<EmailDeliveryJob> logger)
{
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SendAsync(string email, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        try
        {
            await transport.SendAsync(email, subject, htmlBody, textBody, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Email job failed; provider {Provider} will retry. Exception type: {ExceptionType}.", transport.ProviderName, ex.GetType().Name);
            throw new InvalidOperationException("Background email delivery failed; retry scheduled.");
        }
    }
}
