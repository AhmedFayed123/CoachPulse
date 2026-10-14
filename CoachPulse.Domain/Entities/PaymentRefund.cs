namespace CoachPulse.Domain.Entities;

public sealed class PaymentRefund
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? GatewayReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
}
