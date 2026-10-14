using CoachPulse.Api.Hubs;
using CoachPulse.Domain.Entities;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public sealed class WebhooksController : ControllerBase
{
    private static readonly string[] HmacFields =
    [
        "amount_cents", "created_at", "currency", "error_occured",
        "has_parent_transaction", "id", "integration_id", "is_3d_secure",
        "is_auth", "is_capture", "is_refund", "is_standalone_payment",
        "is_void", "order.id", "owner", "pending", "source_data.pan",
        "source_data.sub_type", "source_data.type", "success"
    ];

    private readonly CoachPulseDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;
    private readonly IHubContext<NotificationsHub> _notifications;
    private readonly ISubscriptionActivationService _activation;

    public WebhooksController(
        CoachPulseDbContext context,
        IConfiguration configuration,
        ILogger<WebhooksController> logger,
        IHubContext<NotificationsHub> notifications,
        ISubscriptionActivationService activation)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _notifications = notifications;
        _activation = activation;
    }

    [HttpPost("paymob")]
    [EnableRateLimiting("paymob-webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandlePaymobWebhook(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("obj", out var obj) || obj.ValueKind != JsonValueKind.Object)
            return BadRequest(new { message = "Invalid Paymob payload." });

        var signature = Request.Query["hmac"].ToString();
        if (string.IsNullOrWhiteSpace(signature) || !VerifyHmac(obj, signature))
        {
            _logger.LogWarning("Invalid Paymob HMAC signature detected.");
            return Unauthorized(new { message = "Invalid HMAC signature." });
        }

        if (!TryGetString(obj, "id", out var transactionId) ||
            !TryGetNestedString(obj, "order", "id", out var orderId) ||
            !TryGetString(obj, "amount_cents", out var amountText) ||
            !long.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amountCents) ||
            !TryGetString(obj, "currency", out var currency) ||
            !TryGetString(obj, "success", out var successText) ||
            !bool.TryParse(successText, out var succeeded))
        {
            return BadRequest(new { message = "Paymob payload is missing required transaction fields." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var webhookEvent = new PaymobWebhookEvent
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = transactionId,
            GatewayOrderId = orderId,
            Payload = payload.GetRawText(),
            ReceivedAtUtc = DateTime.UtcNow
        };
        _context.WebhookEvents.Add(webhookEvent);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateTransaction(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogInformation("Ignoring duplicate Paymob transaction {TransactionId}.", transactionId);
            return Ok();
        }

        var payment = await _context.Payments
            .IgnoreQueryFilters()
            .Include(x => x.Subscription)
            .ThenInclude(x => x.Client)
            .Include(x => x.Subscription)
            .ThenInclude(x => x.Plan)
            .FirstOrDefaultAsync(x => x.GatewayReference == orderId, cancellationToken);

        if (payment is null)
        {
            webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogWarning("No payment found for Paymob order {OrderId}.", orderId);
            return Ok();
        }

        var expectedCents = decimal.ToInt64(decimal.Round(payment.Amount * 100m, 0, MidpointRounding.AwayFromZero));
        if (expectedCents != amountCents ||
            !string.Equals(payment.Currency, currency, StringComparison.OrdinalIgnoreCase))
        {
            webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogError(
                "Paymob amount/currency mismatch for payment {PaymentId}. Expected {Amount} {Currency}; received {ReceivedAmount} {ReceivedCurrency}.",
                payment.Id, expectedCents, payment.Currency, amountCents, currency);
            return Ok();
        }

        if (payment.Status != PaymentStatus.Confirmed)
        {
            payment.GatewayTransactionId = transactionId;
            payment.Status = succeeded ? PaymentStatus.Confirmed : PaymentStatus.Failed;
            payment.Version = Guid.NewGuid().ToByteArray();
            if (succeeded && payment.Subscription is not null)
            {
                try
                {
                    await _activation.ActivateAsync(payment.Subscription, SubscriptionActivationSource.PaymobWebhook, cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Paymob payment {PaymentId} could not activate subscription {SubscriptionId}.", payment.Id, payment.SubscriptionId);
                    payment.Status = PaymentStatus.Failed;
                }
            }
        }

        webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var changed = new
        {
            paymentId = payment.Id,
            payment.Status,
            subscriptionId = payment.SubscriptionId
        };
        var client = payment.Subscription?.Client;
        if (client is not null)
        {
            await Notify(client.UserId, payment.TenantId, "PaymentStatusChanged", "حالة الدفع", "تم تحديث حالة دفعتك.", changed, cancellationToken);
            if (succeeded)
            {
                await Notify(client.UserId, payment.TenantId, "SubscriptionStatusChanged", "حالة الاشتراك", "تم تحديث حالة اشتراكك.", new { subscriptionId = payment.SubscriptionId, status = payment.Subscription?.Status }, cancellationToken);
            }
            if (client.CoachId.HasValue)
            {
                await Notify(client.CoachId.Value, payment.TenantId, "PaymentStatusChanged", "تحديث دفعة عميل", "تم تحديث حالة دفعة عميل.", changed, cancellationToken);
            }
        }

        return Ok();
    }

    private bool VerifyHmac(JsonElement obj, string suppliedSignature)
    {
        var secret = _configuration["Paymob:HmacSecret"];
        if (string.IsNullOrWhiteSpace(secret) || suppliedSignature.Length != 128)
            return false;

        var canonical = new StringBuilder();
        foreach (var field in HmacFields)
        {
            if (!TryGetField(obj, field, out var value))
                return false;
            canonical.Append(value);
        }

        var expected = HMACSHA512.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(canonical.ToString()));
        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(suppliedSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }

    private Task Notify(Guid userId, Guid tenantId, string type, string title, string body, object data, CancellationToken ct) =>
        HttpContext.RequestServices.GetRequiredService<INotificationService>().NotifyAsync(userId, tenantId, type, title, body, JsonSerializer.Serialize(data), ct);

    private static bool TryGetField(JsonElement obj, string field, out string value)
    {
        value = string.Empty;
        var parts = field.Split('.');
        var current = obj;
        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                return false;
        }

        value = current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => current.GetRawText(),
            _ => string.Empty
        };
        return current.ValueKind is JsonValueKind.String or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Number;
    }

    private static bool TryGetString(JsonElement obj, string property, out string value) =>
        TryGetField(obj, property, out value);

    private static bool TryGetNestedString(JsonElement obj, string parent, string property, out string value) =>
        TryGetField(obj, $"{parent}.{property}", out value);

    private static bool IsDuplicateTransaction(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
