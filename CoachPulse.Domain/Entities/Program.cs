using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class Program:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid ClientId { get; set; }

        public Guid CoachId { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsTemplate { get; set; }

        public Client Client { get; set; } = null!;

        public User Coach { get; set; } = null!;
    }
}
