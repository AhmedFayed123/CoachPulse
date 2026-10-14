using System.Globalization;
using System.Text.Json;
using CoachPulse.Api.Hubs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.BackgroundJobs;

public sealed class ExpireSubscriptionsJob(CoachPulseDbContext db, ISubscriptionStateMachine states)
{
    [AutomaticRetry(Attempts=3)]
    public async Task RunAsync(CancellationToken ct=default)
    {
        var now=DateTime.UtcNow;
        var tenantIds=await db.Tenants.AsNoTracking().Where(t=>t.Status=="Active").Select(t=>t.Id).ToListAsync(ct);
        foreach(var tenantId in tenantIds)
        {
            var due=await db.Subscriptions.IgnoreQueryFilters().Where(s=>s.TenantId==tenantId&&s.Status==SubscriptionStatus.Active&&s.EndDate<=now).ToListAsync(ct);
            foreach(var sub in due)states.Transition(sub,SubscriptionStatus.Expired,"Subscription period ended.");
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed class SubscriptionExpiryReminderJob(CoachPulseDbContext db,IHubContext<NotificationsHub> hub)
{
    [AutomaticRetry(Attempts=3)]
    public async Task RunAsync(CancellationToken ct=default)
    {
        var tenants=await db.Tenants.AsNoTracking().Where(t=>t.Status=="Active").Select(t=>new{t.Id,t.ReminderDays}).ToListAsync(ct);var now=DateTime.UtcNow;
        foreach(var tenant in tenants)
        {
            var due=await db.Subscriptions.IgnoreQueryFilters().Include(s=>s.Client).Where(s=>s.TenantId==tenant.Id&&s.Status==SubscriptionStatus.Active&&s.EndDate>now&&s.EndDate<=now.AddDays(tenant.ReminderDays)).ToListAsync(ct);
            foreach(var s in due)
            {
                var marker=$"subscription={s.Id};end={s.EndDate:yyyyMMdd}";
                if(await db.SecurityAuditLogs.IgnoreQueryFilters().AnyAsync(a=>a.TenantId==tenant.Id&&a.EventType=="subscription.expiry.reminder"&&a.Identifier==marker,ct))continue;
                db.SecurityAuditLogs.Add(new(){Id=Guid.NewGuid(),TenantId=tenant.Id,EventType="subscription.expiry.reminder",Identifier=marker,CreatedAt=now});
                await hub.Clients.User(s.Client.UserId.ToString()).SendAsync("SubscriptionExpiryReminder",new{subscriptionId=s.Id,endDate=s.EndDate},ct);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed class PendingPaymentReminderJob(CoachPulseDbContext db,IHubContext<NotificationsHub> hub)
{
    [AutomaticRetry(Attempts=3)]
    public async Task RunAsync(CancellationToken ct=default)
    {
        var tenants=await db.Tenants.AsNoTracking().Where(t=>t.Status=="Active").Select(t=>t.Id).ToListAsync(ct);var now=DateTime.UtcNow;
        foreach(var tenant in tenants)
        {
            var payments=await db.Payments.IgnoreQueryFilters().Include(p=>p.Subscription).ThenInclude(s=>s.Client).Where(p=>p.TenantId==tenant&&p.Method==PaymentMethod.InstaPay&&p.Status==PaymentStatus.PendingVerification&&p.CreatedAt<=now.AddHours(-24)).ToListAsync(ct);
            var owners=await db.Users.IgnoreQueryFilters().Where(u=>u.TenantId==tenant&&u.Role==UserRole.Owner&&!u.IsDeleted).Select(u=>u.Id).ToListAsync(ct);
            foreach(var p in payments)
            {
                var bucket=(int)((now-p.CreatedAt).TotalHours/6);var marker=$"payment={p.Id};bucket={bucket}";
                if(await db.SecurityAuditLogs.IgnoreQueryFilters().AnyAsync(a=>a.TenantId==tenant&&a.EventType=="payment.pending.reminder"&&a.Identifier==marker,ct))continue;
                db.SecurityAuditLogs.Add(new(){Id=Guid.NewGuid(),TenantId=tenant,EventType="payment.pending.reminder",Identifier=marker,CreatedAt=now});
                foreach(var owner in owners)await hub.Clients.User(owner.ToString()).SendAsync("PendingPaymentCreated",new{paymentId=p.Id,subscriptionId=p.SubscriptionId,amount=p.Amount,currency=p.Currency},ct);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}

public sealed class PaymobReconciliationJob(CoachPulseDbContext db,ISubscriptionActivationService activation)
{
    [AutomaticRetry(Attempts=3)]
    public async Task RunAsync(CancellationToken ct=default)
    {
        var activeTenants=await db.Tenants.AsNoTracking().Where(t=>t.Status=="Active").Select(t=>t.Id).ToListAsync(ct);
        var events=await db.WebhookEvents.IgnoreQueryFilters().Where(x=>x.ProcessedAtUtc==null).OrderBy(x=>x.ReceivedAtUtc).Take(500).ToListAsync(ct);
        foreach(var e in events)
        {
            var p=await db.Payments.IgnoreQueryFilters().Include(x=>x.Subscription).ThenInclude(x=>x.Plan).FirstOrDefaultAsync(x=>x.GatewayReference==e.GatewayOrderId,ct);
            if(p is null){e.ProcessedAtUtc=DateTime.UtcNow;continue;}
            if(!activeTenants.Contains(p.TenantId)||p.Subscription.TenantId!=p.TenantId){e.ProcessedAtUtc=DateTime.UtcNow;continue;}
            try
            {
                using var doc=JsonDocument.Parse(e.Payload);var o=doc.RootElement.GetProperty("obj");var succeeded=o.GetProperty("success").ValueKind==JsonValueKind.True;
                var amount=o.GetProperty("amount_cents").GetInt64();var currency=o.GetProperty("currency").GetString();
                if(amount!=decimal.ToInt64(decimal.Round(p.Amount*100m,0,MidpointRounding.AwayFromZero))||!string.Equals(currency,p.Currency,StringComparison.OrdinalIgnoreCase)){e.ProcessedAtUtc=DateTime.UtcNow;continue;}
                if(succeeded&&p.Status!=PaymentStatus.Confirmed){p.Status=PaymentStatus.Confirmed;await activation.ActivateAsync(p.Subscription,SubscriptionActivationSource.PaymobWebhook,ct);}else if(!succeeded&&p.Status==PaymentStatus.Pending)p.Status=PaymentStatus.Failed;
                e.ProcessedAtUtc=DateTime.UtcNow;
            }
            catch(JsonException){e.ProcessedAtUtc=DateTime.UtcNow;}
            catch(InvalidOperationException){e.ProcessedAtUtc=DateTime.UtcNow;}
        }
        await db.SaveChangesAsync(ct);
    }
}
