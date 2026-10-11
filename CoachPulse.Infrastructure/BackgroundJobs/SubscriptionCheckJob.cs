using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Infrastructure.BackgroundJobs;

public class SubscriptionCheckJob
{
    private readonly CoachPulseDbContext _context;
    private readonly ILogger<SubscriptionCheckJob> _logger;

    public SubscriptionCheckJob(CoachPulseDbContext context, ILogger<SubscriptionCheckJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CheckExpiringSubscriptionsAsync()
    {
        _logger.LogInformation("Starting background job: Checking expiring subscriptions.");

        var targetDate = DateTime.UtcNow.AddDays(3); // الاشتراكات التي ستنتهي خلال 3 أيام[cite: 6, 15, 17]

        var expiringSubscriptions = await _context.Subscriptions
            .Include(s => s.Client)
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= targetDate && s.EndDate > DateTime.UtcNow)
            .ToListAsync();

        foreach (var subscription in expiringSubscriptions)
        {
            // منطق إرسال التنبيه للعميل والمدرب (عبر إشعارات النظام أو الإيميل)[cite: 6, 15, 17]
            _logger.LogInformation("Subscription ID {SubId} for client {ClientId} is expiring soon on {EndDate}.",
                subscription.Id, subscription.ClientId, subscription.EndDate);
        }

        await _context.SaveChangesAsync();
    }
}