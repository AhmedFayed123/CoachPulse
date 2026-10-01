using CoachPulse.Domain.Enums;
using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class User:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
