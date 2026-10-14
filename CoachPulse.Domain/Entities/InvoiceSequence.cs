using CoachPulse.Domain.Interfaces;

namespace CoachPulse.Domain.Entities;

public sealed class InvoiceSequence : ITenantEntity
{
    public Guid TenantId { get; set; }
    public int NextNumber { get; set; } = 1;
}
