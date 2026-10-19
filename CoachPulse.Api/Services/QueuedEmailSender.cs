using System.Net;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Services;
using CoachPulse.Api.BackgroundJobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Api.Services;

public sealed class QueuedEmailSender(IBackgroundJobClient jobs, ILogger<QueuedEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        var safe = WebUtility.HtmlEncode(body);
        var html = $"<!doctype html><html><body><main style=\"font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px\"><h1>{WebUtility.HtmlEncode(subject)}</h1><p>{safe.Replace("\r\n", "<br>").Replace("\n", "<br>")}</p></main></body></html>";
        return Enqueue(email, subject, html, body);
    }

    public Task SendTemplateAsync(string email, EmailTemplateType type, string? language, IReadOnlyDictionary<string, string>? values = null, CancellationToken cancellationToken = default)
    {
        var content = EmailTemplateRenderer.Render(type, language, values);
        return Enqueue(email, content.Subject, content.HtmlBody, content.TextBody);
    }

    private Task Enqueue(string email, string subject, string html, string text)
    {
        try
        {
            jobs.Enqueue<EmailDeliveryJob>(job => job.SendAsync(email, subject, html, text, CancellationToken.None));
        }
        catch (Exception ex)
        {
            // Delivery infrastructure must never turn an otherwise successful HTTP request into an error.
            logger.LogError("Could not enqueue email delivery. Exception type: {ExceptionType}.", ex.GetType().Name);
        }
        return Task.CompletedTask;
    }
}
