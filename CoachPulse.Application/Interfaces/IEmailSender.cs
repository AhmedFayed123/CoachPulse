namespace CoachPulse.Application.Interfaces;

using CoachPulse.Application.DTOs;
using CoachPulse.Application.Services;

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default);

    Task SendTemplateAsync(string email, EmailTemplateType type, string? language, IReadOnlyDictionary<string, string>? values = null, CancellationToken cancellationToken = default)
    {
        var content = EmailTemplateRenderer.Render(type, language, values);
        return SendAsync(email, content.Subject, content.TextBody, cancellationToken);
    }
}
