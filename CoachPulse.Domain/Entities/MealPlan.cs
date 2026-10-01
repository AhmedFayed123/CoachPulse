using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class MealPlan:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid ClientId { get; set; }

        public string Meals { get; set; } = string.Empty;
    }
}
