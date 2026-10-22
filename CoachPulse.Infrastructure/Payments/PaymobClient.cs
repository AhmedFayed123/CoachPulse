using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoachPulse.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Infrastructure.Payments;

public class PaymobClient : IPaymobClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymobClient> _logger;

    public PaymobClient(HttpClient httpClient, IConfiguration configuration, ILogger<PaymobClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymobIntentionResult> CreatePaymentIntentionAsync(
        decimal amount,
        string currency,
        string orderId,
        string billingEmail,
        string billingFirstName,
        string billingLastName,
        string billingPhone,
        CancellationToken cancellationToken)
    {
        var secretKey = RequiredSetting("Paymob:SecretKey");
        var publicKey = RequiredSetting("Paymob:PublicKey");
        var integrationValue = _configuration["Paymob:IntegrationId"];
        if (string.IsNullOrWhiteSpace(integrationValue))
            throw new InvalidOperationException("Paymob:IntegrationId is not configured.");
        if (!int.TryParse(integrationValue.Trim(), out var integrationId))
            throw new InvalidOperationException("Paymob:IntegrationId must be a numeric integration ID.");
        var baseUrl = GetPaymobBaseUrl(_configuration["Paymob:BaseUrl"]);

        // تحويل المبلغ إلى قروش (Cents) حسب متطلبات Paymob
        var amountCents = (int)(amount * 100);

        var returnUrl = _configuration["Paymob:ReturnUrl"];
        var notificationUrl = _configuration["Paymob:NotificationUrl"];
        var requestBody = new
        {
            amount = amountCents,
            currency = currency,
            payment_methods = new[] { integrationId },
            items = new[] { new { name = "Subscription", amount = amountCents, description = "CoachPulse subscription", quantity = 1 } },
            billing_data = new
            {
                email = billingEmail,
                first_name = billingFirstName,
                last_name = billingLastName,
                phone_number = billingPhone,
                apartment = "NA",
                floor = "NA",
                street = "NA",
                building = "NA",
                shipping_method = "NA",
                postal_code = "NA",
                city = "NA",
                country = "EG",
                state = "NA"
            },
            special_reference = orderId,
            redirection_url = string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl,
            notification_url = string.IsNullOrWhiteSpace(notificationUrl) ? null : notificationUrl
        };

        // إعداد الـ Authorization Header بالـ Secret Key
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", secretKey);

        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(baseUrl, "v1/intention/").ToString(),
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaymobIntentionResponse>(cancellationToken: cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.ClientSecret) || result.IntentionOrderId.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new InvalidOperationException("Paymob response did not include intention order id/client secret.");
        var checkoutUrl = BuildUnifiedCheckoutUrl(baseUrl, publicKey, result.ClientSecret);
        _logger.LogInformation("Created Paymob intention for order {OrderId}.", orderId);
        return new PaymobIntentionResult(checkoutUrl, result.IntentionOrderId.ToString());
    }

    private string RequiredSetting(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value) || value.Contains("leave-empty", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{key} is not configured.");
        return value.Trim();
    }

    internal static Uri GetPaymobBaseUrl(string? configuredBaseUrl)
    {
        var candidate = string.IsNullOrWhiteSpace(configuredBaseUrl) ? "https://accept.paymob.com/" : configuredBaseUrl.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var baseUrl) || baseUrl.Scheme != Uri.UriSchemeHttps ||
            baseUrl.UserInfo.Length > 0 || baseUrl.Query.Length > 0 || baseUrl.Fragment.Length > 0 ||
            baseUrl.AbsolutePath != "/" || baseUrl.Host is not ("accept.paymob.com" or "oman.paymob.com" or "ksa.paymob.com" or "uae.paymob.com"))
            throw new InvalidOperationException("Paymob:BaseUrl must be an HTTPS Paymob regional host (accept, oman, ksa, or uae). ");
        return baseUrl;
    }

    internal static string BuildUnifiedCheckoutUrl(Uri baseUrl, string publicKey, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("Paymob public key and intention client secret are required to build checkout URL.");
        return $"{baseUrl.GetLeftPart(UriPartial.Authority)}/unifiedcheckout/?publicKey={Uri.EscapeDataString(publicKey)}&clientSecret={Uri.EscapeDataString(clientSecret)}";
    }

    public async Task<string> RefundAsync(string transactionId, decimal amount, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Paymob:ApiKey"] ?? throw new InvalidOperationException("Paymob ApiKey is required for refunds.");
        using var authResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens", new { api_key = apiKey }, cancellationToken);
        authResponse.EnsureSuccessStatusCode();
        var auth = await authResponse.Content.ReadFromJsonAsync<PaymobAuthResponse>(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(auth?.Token)) throw new InvalidOperationException("Paymob did not return an auth token.");
        var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/void_refund/refund", new { auth_token = auth.Token, transaction_id = transactionId, amount_cents = decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)) }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

internal class PaymobIntentionResponse
{
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;
    [JsonPropertyName("intention_order_id")]
    public JsonElement IntentionOrderId { get; set; }
}

internal class PaymobAuthResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
