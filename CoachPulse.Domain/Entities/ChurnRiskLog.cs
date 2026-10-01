using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class ChurnRiskLog:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid ClientId { get; set; }

        public decimal RiskScore { get; set; }

        public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

        public Client Client { get; set; } = null!;
    }
}
