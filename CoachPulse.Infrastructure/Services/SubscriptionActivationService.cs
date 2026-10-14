using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;

namespace CoachPulse.Infrastructure.Services;

public sealed class SubscriptionActivationService : ISubscriptionActivationService, ISubscriptionStateMachine
{
    public Task ActivateAsync(Subscription subscription, SubscriptionActivationSource source, CancellationToken cancellationToken = default)
    {
        if (source is not (SubscriptionActivationSource.PaymobWebhook or SubscriptionActivationSource.InstaPayConfirmation or SubscriptionActivationSource.OwnerManual))
            throw new InvalidOperationException("Subscription activation source is not permitted.");
        if (subscription.Status != SubscriptionStatus.Pending)
            throw new InvalidOperationException("Only a pending subscription can be activated.");
        subscription.Status = SubscriptionStatus.Active;
        subscription.StartDate = DateTime.UtcNow;
        var duration = subscription.SnapshotDurationDays > 0 ? subscription.SnapshotDurationDays : subscription.Plan?.DurationDays ?? 0;
        if (duration <= 0) throw new InvalidOperationException("Subscription duration is invalid.");
        subscription.EndDate = subscription.StartDate.AddDays(duration);
        subscription.StateChangeReason = source.ToString();
        return Task.CompletedTask;
    }

    public void Transition(Subscription subscription, SubscriptionStatus next, string? reason = null)
    {
        var allowed = (subscription.Status, next) switch
        {
            (SubscriptionStatus.Pending, SubscriptionStatus.Cancelled) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.Paused or SubscriptionStatus.Expired or SubscriptionStatus.Cancelled) => true,
            (SubscriptionStatus.Paused, SubscriptionStatus.Active or SubscriptionStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException($"Illegal subscription transition: {subscription.Status} -> {next}.");
        if (subscription.Status == SubscriptionStatus.Active && next == SubscriptionStatus.Paused)
        {
            subscription.RemainingDays = Math.Max(0, (int)Math.Ceiling((subscription.EndDate - DateTime.UtcNow).TotalDays));
            subscription.PausedAt = DateTime.UtcNow;
        }
        if (subscription.Status == SubscriptionStatus.Paused && next == SubscriptionStatus.Active)
        {
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = subscription.StartDate.AddDays(subscription.RemainingDays ?? 0);
            subscription.RemainingDays = null;
            subscription.PausedAt = null;
        }
        subscription.Status = next;
        subscription.StateChangeReason = reason;
    }
}
