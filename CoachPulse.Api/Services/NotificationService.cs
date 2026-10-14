using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CoachPulse.Api.Hubs;
using System.Text.Json;

namespace CoachPulse.Api.Services;

public sealed class NotificationService(CoachPulseDbContext db, IHubContext<NotificationsHub> hub, IEmailSender emailSender) : INotificationService
{
    private static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase) { "PaymentStatusChanged", "PendingPaymentCreated", "SecurityAlert", "PaymentConfirmed", "PaymentRejected" };

    public async Task NotifyAsync(Guid userId, Guid tenantId, string type, string title, string body, string? dataJson = null, CancellationToken cancellationToken = default)
    {
        var preference = await db.NotificationPreferences.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId && x.Type == type, cancellationToken);
        var critical = Critical.Contains(type);
        if (preference?.InAppEnabled != false || critical)
        {
            var notification = new Notification { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, Type = type, Title = title, Body = body, DataJson = dataJson };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync(cancellationToken);
            await hub.Clients.User(userId.ToString()).SendAsync("NotificationReceived", new { notification.Id, notification.Type, notification.Title, notification.Body, notification.DataJson, notification.CreatedAt }, cancellationToken);
            if (type is "CheckInSubmitted" or "CheckInFeedback" or "MessageReceived" or "PaymentStatusChanged" or "PendingPaymentCreated" or "SubscriptionStatusChanged" or "SubscriptionExpiryReminder" or "ChurnRiskAlert")
            {
                object payload = new { notification.Id, notification.DataJson };
                try { if (!string.IsNullOrWhiteSpace(dataJson)) payload = JsonSerializer.Deserialize<JsonElement>(dataJson); } catch (JsonException) { }
                await hub.Clients.User(userId.ToString()).SendAsync(type, payload, cancellationToken);
            }
            var unread = await db.Notifications.IgnoreQueryFilters().CountAsync(x => x.UserId == userId && x.TenantId == tenantId && !x.IsRead && !x.IsDeleted, cancellationToken);
            await hub.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdated", new { count = unread }, cancellationToken);
        }

        if ((preference?.EmailEnabled ?? true) || critical)
        {
            var email = await db.Users.IgnoreQueryFilters().Where(x => x.Id == userId && x.TenantId == tenantId && !x.IsDeleted).Select(x => x.Email).FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(email)) await emailSender.SendAsync(email, title, body, cancellationToken);
        }
    }
}
