using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.Interfaces
{
    public interface IPaymobClient
    {
        Task<string> CreatePaymentIntentionAsync(
        decimal amount,
        string currency,
        string orderId,
        string billingEmail,
        string billingFirstName,
        string billingLastName,
        string billingPhone,
        CancellationToken cancellationToken);
    }
}
