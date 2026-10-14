using CoachPulse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public Guid? TenantId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string Language { get; set; } = "en";
        public Guid? AvatarFileId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool EmailConfirmed { get; set; }

        public Tenant? Tenant { get; set; }
    }
}
