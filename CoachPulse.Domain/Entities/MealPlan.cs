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

        public Guid? ClientId { get; set; }

        public string Meals { get; set; } = string.Empty;

        public string Title { get; set; } = "Meal plan";
        public bool IsTemplate { get; set; }
        public string Status { get; set; } = "Draft";
        public byte[] RowVersion { get; set; } = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Meal> Items { get; set; } = new List<Meal>();

        public Client? Client { get; set; }
    }
}
