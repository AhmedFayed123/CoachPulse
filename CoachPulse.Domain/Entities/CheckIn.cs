using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class CheckIn:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid ClientId { get; set; }

        public DateTime Date { get; set; }

        public decimal Weight { get; set; }

        public string? Measurements { get; set; }

        public string? PhotoUrl { get; set; }
    }
}
