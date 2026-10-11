using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymobClient _paymobClient;
    private readonly CoachPulseDbContext _context;

    public PaymentsController(IPaymobClient paymobClient, CoachPulseDbContext context)
    {
        _paymobClient = paymobClient;
        _context = context;
    }

    // POST: /api/payments/paymob/create-intention
    [HttpPost("paymob/create-intention")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> CreatePaymobIntention(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        // 1. استخراج بيانات المستخدم والـ Tenant
        // 2. التحقق من الباقة المبلغ المطلوب
        // 3. استدعاء خدمة PaymobClient لإنشاء نية الدفع

        var checkoutUrl = await _paymobClient.CreatePaymentIntentionAsync(
            request.Amount,
            "EGP",
            Guid.NewGuid().ToString(),
            "client@test.com",
            "Ahmed",
            "Fayed",
            "+201000000000",
            cancellationToken);

        return Ok(new { CheckoutUrl = checkoutUrl });
    }
}

public record CreatePaymentRequest(Guid PlanId, decimal Amount);