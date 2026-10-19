using System.Net;
using System.Text;
using CoachPulse.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class PaymobClientTests
{
    [Fact]
    public async Task IntentionBuildsOfficialUnifiedCheckoutUrlAndKeepsSecretsOutOfLogs()
    {
        const string clientSecret = "csk_test_client-secret-value";
        const string apiSecret = "sk_test_api-secret-value";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"client_secret":"csk_test_client-secret-value","intention_order_id":98765}""", Encoding.UTF8, "application/json")
        });
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Trace));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Paymob:SecretKey"] = apiSecret,
            ["Paymob:PublicKey"] = "egy_pk_test_public-only",
            ["Paymob:IntegrationId"] = "12345",
            ["Paymob:BaseUrl"] = "https://accept.paymob.com",
            ["Paymob:ReturnUrl"] = "https://app.example.test/my/payment/return",
            ["Paymob:NotificationUrl"] = "https://api.example.test/api/webhooks/paymob"
        }).Build();
        var http = new HttpClient(handler);
        var client = new PaymobClient(http, config, loggerFactory.CreateLogger<PaymobClient>());

        var intention = await client.CreatePaymentIntentionAsync(123.45m,"EGP","order-987","member@example.test","Member","Test","+201000000000",CancellationToken.None);

        Assert.Equal("98765", intention.OrderId);
        Assert.StartsWith("https://accept.paymob.com/unifiedcheckout/?", intention.CheckoutUrl, StringComparison.Ordinal);
        Assert.Contains("publicKey=egy_pk_test_public-only", intention.CheckoutUrl, StringComparison.Ordinal);
        Assert.Contains("clientSecret=" + Uri.EscapeDataString(clientSecret), intention.CheckoutUrl, StringComparison.Ordinal);
        Assert.DoesNotContain(apiSecret, intention.CheckoutUrl, StringComparison.Ordinal);
        Assert.NotNull(handler.LastRequestHeaders);
        Assert.True(handler.LastRequestHeaders!.TryGetValue("Authorization",out var authorizationHeader));
        Assert.Equal("Token " + apiSecret, authorizationHeader!.Single());
        Assert.DoesNotContain(loggerProvider.Messages, message => message.Contains(clientSecret, StringComparison.Ordinal) || message.Contains(apiSecret, StringComparison.Ordinal) || message.Contains(intention.CheckoutUrl, StringComparison.Ordinal));
        Assert.Contains("https://app.example.test/my/payment/return", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("https://api.example.test/api/webhooks/paymob", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    private sealed class StubHandler(Func<HttpRequestMessage,HttpResponseMessage> response) : HttpMessageHandler
    {
        public Dictionary<string,string[]>? LastRequestHeaders { get; private set; }
        public string? LastRequestBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            LastRequestHeaders=request.Headers.ToDictionary(x=>x.Key,x=>x.Value.ToArray(),StringComparer.OrdinalIgnoreCase);
            LastRequestBody=request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return response(request);
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; }=[];
        public ILogger CreateLogger(string categoryName)=>new CapturingLogger(Messages);
        public void Dispose() { }
    }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(LogLevel logLevel)=>true;
        public void Log<TState>(LogLevel logLevel,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
            =>messages.Add(formatter(state,exception));
    }
}
