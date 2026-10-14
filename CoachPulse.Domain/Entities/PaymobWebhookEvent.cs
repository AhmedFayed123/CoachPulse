namespace CoachPulse.Domain.Entities;

public sealed class PaymobWebhookEvent
{
    public Guid Id { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
