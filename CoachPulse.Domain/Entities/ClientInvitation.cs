using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class ClientInvitation
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid CoachId { get; set; }

        public string Email { get; set; } = string.Empty;

        // Store the hash, never the raw invitation token.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}
