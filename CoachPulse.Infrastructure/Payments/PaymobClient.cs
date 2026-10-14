using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoachPulse.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoachPulse.Infrastructure.Payments;

public class PaymobClient : IPaymobClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PaymobClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
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
        var secretKey = _configuration["Paymob:SecretKey"]
            ?? throw new InvalidOperationException("Paymob SecretKey is missing.");

        var integrationId = _configuration["Paymob:IntegrationId"]
            ?? throw new InvalidOperationException("Paymob IntegrationId is missing.");

        // تحويل المبلغ إلى قروش (Cents) حسب متطلبات Paymob
        var amountCents = (int)(amount * 100);

        var requestBody = new
        {
            amount = amountCents,
            currency = currency,
            payment_methods = new[] { int.Parse(integrationId) },
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
            special_reference = orderId
        };

        // إعداد الـ Authorization Header بالـ Secret Key
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", secretKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://accept.paymob.com/v1/intention",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaymobIntentionResponse>(cancellationToken: cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.ClientSecret) || result.IntentionOrderId.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new InvalidOperationException("Paymob response did not include intention order id/client secret.");
        return new PaymobIntentionResult(result.ClientSecret, result.IntentionOrderId.ToString());
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
