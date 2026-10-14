using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionScheduledAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Currency { get; set; } = "EGP";
        public string TimeZone { get; set; } = "Africa/Cairo";
        public int ReminderDays { get; set; } = 7;
        public string DefaultLanguage { get; set; } = "ar";
        public bool PaymobEnabled { get; set; }
        public bool InstaPayEnabled { get; set; }
        public string? InstaPayId { get; set; }
        public string? InstaPayAccountNumber { get; set; }
    }
}
