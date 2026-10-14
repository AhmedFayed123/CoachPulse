using CoachPulse.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class AIGeneratedProgram:ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid ClientId { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public string GeneratedContent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool ApprovedByCoach { get; set; }
        public string Status { get; set; } = "Draft";
        public string PromptVersion { get; set; } = "program-v1";
        public string ModelVersion { get; set; } = "unknown";
        public Guid RequestedByUserId { get; set; }
        public Guid? ParentGeneratedProgramId { get; set; }
        public Guid? ApprovedProgramId { get; set; }
        public Guid? MealPlanId { get; set; }
        public byte[] RowVersion { get; set; } = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);

        public Client Client { get; set; } = null!;
    }
}
