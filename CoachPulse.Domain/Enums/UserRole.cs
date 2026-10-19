using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Enums
{
    public enum UserRole
    {
        SuperAdmin = 1,
        TenantOwner = 2,
        CoachStaff = 3,
        Client = 4
    }
}
