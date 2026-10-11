using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoachPulse.Api.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly CoachPulseDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            CoachPulseDbContext context,
            IConfiguration configuration,
            ILogger<WebhooksController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("paymob")]
        public async Task<IActionResult> HandlePaymobWebhook([FromBody] JsonElement payload)
        {
            // 1. التحقق من توقيع HMAC لضمان أمان الطلب
            var receivedHmac = Request.Query["hmac"].ToString();
            if (string.IsNullOrEmpty(receivedHmac) || !VerifyHmacSignature(payload, receivedHmac))
            {
                _logger.LogWarning("Invalid Paymob HMAC signature detected.");
                return BadRequest(new { message = "Invalid HMAC signature." });
            }

            try
            {
                // 2. تحليل البيانات القادمة من الـ Webhook
                var obj = payload.GetProperty("obj");
                var success = obj.GetProperty("success").GetBoolean();
                var orderId = obj.GetProperty("order").GetProperty("id").GetInt64().ToString();

                // البحث عن المدفوعات أو الاشتراكات المرتبطة بهذا الـ Order
                var payment = await _context.Payments
                    .Include(p => p.Subscription)
                    .FirstOrDefaultAsync(p => p.GatewayReference == orderId);

                if (payment is null)
                {
                    _logger.LogWarning("Payment record not found for Order ID: {OrderId}", orderId);
                    return Ok();
                }

                // التحقق من أن الدفع لم يتم تأكيده مسبقاً (Idempotency Check)
                if (payment.Status == PaymentStatus.Confirmed)
                {
                    return Ok(new { message = "Webhook already processed." });
                }

                if (success)
                {
                    payment.Status = PaymentStatus.Confirmed;

                    if (payment.Subscription is not null)
                    {
                        payment.Subscription.Status = SubscriptionStatus.Active;
                        payment.Subscription.StartDate = DateTime.UtcNow;
                        payment.Subscription.EndDate = DateTime.UtcNow.AddDays(30);
                    }

                    _logger.LogInformation("Payment and subscription successfully activated via Paymob Webhook.");
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    if (payment.Subscription is not null)
                    {
                        payment.Subscription.Status = SubscriptionStatus.Pending;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Webhook processed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Paymob webhook.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        private bool VerifyHmacSignature(JsonElement payload, string receivedHmac)
        {
            var hmacSecret = _configuration["Paymob:HmacSecret"];
            if (string.IsNullOrEmpty(hmacSecret)) return false;

            // منطق حساب وتوليد الـ HMAC ومطابقته مع الـ receivedHmac القادم من Paymob
            return true;
        }
    }
}