using CoachPulse.Domain.Interfaces;
using CoachPulse.Domain.Enums;
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

        public ClientStatus Status { get; set; } = ClientStatus.Active;
        public CheckInFrequency? CheckInFrequency { get; set; }
        public DayOfWeek? CheckInDayOfWeek { get; set; }

        public string? Goals { get; set; }

        public string? HealthInfo { get; set; }
        public bool OnboardingCompleted { get; set; }

        public User User { get; set; } = null!;

        public User? Coach { get; set; }
    }
}

