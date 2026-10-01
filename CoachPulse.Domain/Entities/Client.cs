using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class Client:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid UserId { get; set; }

        public Guid? CoachId { get; set; }

        public string? Goals { get; set; }

        public string? HealthInfo { get; set; }

        public User User { get; set; } = null!;

        public User? Coach { get; set; }
    }
}
