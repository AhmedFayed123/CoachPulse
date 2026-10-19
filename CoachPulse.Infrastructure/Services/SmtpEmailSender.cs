using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CoachPulse.Infrastructure.Services;

public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailDeliveryTransport
{
    public string ProviderName => "Smtp";

    public async Task SendAsync(string email, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        var host = configuration["Email:Smtp:Host"];
        var port = configuration.GetValue<int?>("Email:Smtp:Port");
        var fromAddress = configuration["Email:Smtp:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || port is null or < 1 or > 65535 || string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Email SMTP Host, Port, and FromAddress are required when Email:Provider=Smtp.");
        if (!MailboxAddress.TryParse(fromAddress, out var from))
            throw new InvalidOperationException("Email SMTP FromAddress must be a valid mailbox address.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(configuration["Email:Smtp:FromName"] ?? "CoachPulse", from.Address));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var useSsl = configuration.GetValue<bool>("Email:Smtp:UseSsl");
            await client.ConnectAsync(host, port.Value, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
            var username = configuration["Email:Smtp:Username"];
            var password = configuration["Email:Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username))
                await client.AuthenticateAsync(username, password ?? string.Empty, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The queued Hangfire job records retry state; never log recipient, message body, or token-bearing links.
            logger.LogWarning("SMTP delivery failed; Hangfire will retry. Exception type: {ExceptionType}.", ex.GetType().Name);
            throw;
        }
    }
}
