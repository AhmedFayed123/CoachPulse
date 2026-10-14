using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class Program : ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid? ClientId { get; set; }

        public Guid CoachId { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsTemplate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Draft";
        public DateTime? StartDate { get; set; }
        public int DurationDays { get; set; } = 7;
        public byte[] RowVersion { get; set; } = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
        public bool IsDeleted { get; set; }

        public Client? Client { get; set; }

        public User Coach { get; set; } = null!;
    }
}
