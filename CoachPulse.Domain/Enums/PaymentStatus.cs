using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending = 1,
        Confirmed = 2,
        Failed = 3,
        Refunded = 4
    }
}
