using System.Collections.Concurrent;
using CoachPulse.Api.BackgroundJobs;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Services;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class EmailDeliveryTests
{
    [Fact]
    public async Task TemplatesRenderBothLanguagesAndEncodeUntrustedValues()
    {
        var values = new Dictionary<string, string>
        {
            ["name"] = "<script>alert(1)</script>",
            ["token"] = "private-token<&>",
            ["detail"] = "Client supplied <b>text</b>"
        };
        var transport = new FakeTransport();
        var job = new EmailDeliveryJob(transport, new SilentLogger<EmailDeliveryJob>());

        foreach (var type in Enum.GetValues<EmailTemplateType>())
        foreach (var language in new[] { "ar", "en" })
        {
            var content = EmailTemplateRenderer.Render(type, language, values);
            await job.SendAsync("client@example.test", content.Subject, content.HtmlBody, content.TextBody, CancellationToken.None);
            Assert.Contains(language == "ar" ? "dir=\"rtl\"" : "dir=\"ltr\"", content.HtmlBody);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", content.HtmlBody);
            Assert.Contains("private-token&lt;&amp;&gt;", content.HtmlBody);
            Assert.DoesNotContain("<script>", content.HtmlBody);
            Assert.Contains("private-token<&>", content.TextBody);
        }

        Assert.Equal(12, transport.Sent.Count);
        Assert.Equal("تأكيد البريد الإلكتروني", EmailTemplateRenderer.Render(EmailTemplateType.EmailConfirmation, "ar").Subject);
        Assert.Equal("Confirm your email", EmailTemplateRenderer.Render(EmailTemplateType.EmailConfirmation, "en").Subject);
    }

    [Fact]
    public async Task DevelopmentTransportNeverLogsMessageTokensOrLinks()
    {
        var logger = new CaptureLogger<DevelopmentEmailSender>();
        var sender = new DevelopmentEmailSender(logger, new FakeEnvironment("Development"));
        await sender.SendAsync("user@example.test", "Reset password", "<p>sensitive-token-and-link</p>", "sensitive-token-and-link", CancellationToken.None);
        Assert.DoesNotContain("sensitive-token-and-link", logger.Output);
        Assert.DoesNotContain("user@example.test", logger.Output);
        Assert.Contains("Development email queued", logger.Output);
    }

    private sealed class FakeTransport : IEmailDeliveryTransport
    {
        public string ProviderName => "Fake SMTP";
        public ConcurrentBag<(string To, string Subject, string Html, string Text)> Sent { get; } = [];
        public Task SendAsync(string email, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
        {
            Sent.Add((email, subject, htmlBody, textBody));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CoachPulse.Tests";
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        private readonly List<string> _messages = [];
        public string Output => string.Join("\n", _messages);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));
    }

    private sealed class SilentLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
