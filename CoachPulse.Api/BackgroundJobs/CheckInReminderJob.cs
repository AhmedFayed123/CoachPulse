using System.Text.Json;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachPulse.Api.BackgroundJobs;

public sealed class CheckInReminderJob(CoachPulseDbContext db, INotificationService notifications, ILogger<CheckInReminderJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var tenants = await db.Tenants.IgnoreQueryFilters().AsNoTracking().Where(t => t.Status == "Active" && !t.IsDeleted).Select(t => new { t.Id, t.TimeZone }).ToListAsync(ct);
        foreach (var tenant in tenants)
        {
            try
            {
                TimeZoneInfo zone;
                try { zone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZone); }
                catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
                catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
                var now = DateTime.UtcNow;
                var localToday = TimeZoneInfo.ConvertTimeFromUtc(now, zone).Date;
                var clients = await db.Clients.IgnoreQueryFilters().Include(c => c.User).Where(c => c.TenantId == tenant.Id && c.Status == ClientStatus.Active && c.CheckInFrequency != null && c.CheckInDayOfWeek == localToday.DayOfWeek && db.Subscriptions.IgnoreQueryFilters().Any(s => s.TenantId == tenant.Id && s.ClientId == c.Id && s.Status == SubscriptionStatus.Active && s.EndDate > now)).ToListAsync(ct);
                foreach (var client in clients)
                {
                    var latest = await db.CheckIns.IgnoreQueryFilters().Where(c => c.TenantId == tenant.Id && c.ClientId == client.Id && !c.IsDeleted).OrderByDescending(c => c.Date).Select(c => (DateTime?)c.Date).FirstOrDefaultAsync(ct);
                    var localLast = latest.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(latest.Value, DateTimeKind.Utc), zone).Date : (DateTime?)null;
                    var due = client.CheckInFrequency switch
                    {
                        CheckInFrequency.Weekly => !localLast.HasValue || (localToday - localLast.Value).TotalDays >= 7,
                        CheckInFrequency.BiWeekly => !localLast.HasValue || (localToday - localLast.Value).TotalDays >= 14,
                        CheckInFrequency.Monthly => !localLast.HasValue || localToday >= localLast.Value.AddMonths(1),
                        _ => false
                    };
                    if (!due) continue;
                    var marker = $"client={client.Id};day={localToday:yyyyMMdd}";
                    if (await db.SecurityAuditLogs.IgnoreQueryFilters().AnyAsync(a => a.TenantId == tenant.Id && a.EventType == "checkin.reminder.sent" && a.Identifier == marker, ct)) continue;
                    db.SecurityAuditLogs.Add(new() { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = client.UserId, EventType = "checkin.reminder.sent", Action = "checkin.reminder.sent", EntityType = "Client", EntityId = client.Id.ToString(), Identifier = marker, CreatedAt = now });
                    await db.SaveChangesAsync(ct);
                    await notifications.NotifyAsync(client.UserId, tenant.Id, "CheckInReminder", "موعد تسجيل المتابعة", "حان موعد تسجيل متابعة تقدمك.", JsonSerializer.Serialize(new { clientId = client.Id }), ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Check-in reminder failed for tenant {TenantId}.", tenant.Id);
            }
        }
    }
}
