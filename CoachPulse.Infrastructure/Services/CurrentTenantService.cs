using CoachPulse.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Infrastructure.Services
{
    public class CurrentTenantService : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; }

        public void SetTenant(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public void Clear()
        {
            TenantId = null;
        }
    }
}
