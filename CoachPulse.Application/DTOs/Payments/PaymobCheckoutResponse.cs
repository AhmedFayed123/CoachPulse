namespace CoachPulse.Application.DTOs.Payments;

public sealed record PaymobCheckoutResponse(
    Guid PaymentId,
    Guid SubscriptionId,
    string CheckoutUrl,
    string PaymentOrderId,
    decimal Amount,
    string Currency);
