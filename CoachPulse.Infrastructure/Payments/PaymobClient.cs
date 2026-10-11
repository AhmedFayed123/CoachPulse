using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoachPulse.Application.Interfaces;
using Microsoft.Extensions.Configuration;

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

    public async Task<string> CreatePaymentIntentionAsync(
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
            extras = new
            {
                order_id = orderId
            }
        };

        // إعداد الـ Authorization Header بالـ Secret Key
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", secretKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://accept.paymob.com/v1/intention",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaymobIntentionResponse>(cancellationToken: cancellationToken);

        return result?.ClientSecret ?? throw new InvalidOperationException("Failed to create Paymob payment intention.");
    }
}

internal class PaymobIntentionResponse
{
    public string ClientSecret { get; set; } = string.Empty;
}