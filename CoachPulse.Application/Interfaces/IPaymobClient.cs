using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.Interfaces
{
    public sealed record PaymobIntentionResult(string ClientSecret, string OrderId);
    public interface IPaymobClient
    {
        Task<PaymobIntentionResult> CreatePaymentIntentionAsync(
        decimal amount,
        string currency,
        string orderId,
        string billingEmail,
        string billingFirstName,
        string billingLastName,
        string billingPhone,
        CancellationToken cancellationToken);

        Task<string> RefundAsync(string transactionId, decimal amount, CancellationToken cancellationToken);
    }
}
