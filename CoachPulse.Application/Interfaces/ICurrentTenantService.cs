using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.Interfaces
{
    public interface ICurrentTenantService
    {
        Guid? TenantId { get; }
    }
}
