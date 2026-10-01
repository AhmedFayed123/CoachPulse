using CoachPulse.Domain.Enums;
using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class Payment:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid SubscriptionId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "EGP";

        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; }

        public string? GatewayReference { get; set; }

        public string? ReceiptUrl { get; set; }

        public Guid? ConfirmedByAdminId { get; set; }

        public Subscription Subscription { get; set; } = null!;

        public User? ConfirmedByAdmin { get; set; }
    }
}
