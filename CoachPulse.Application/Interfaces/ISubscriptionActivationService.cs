using CoachPulse.Domain.Entities;

namespace CoachPulse.Application.Interfaces;

public enum SubscriptionActivationSource { PaymobWebhook, InstaPayConfirmation, OwnerManual }

public interface ISubscriptionActivationService
{
    Task ActivateAsync(Subscription subscription, SubscriptionActivationSource source, CancellationToken cancellationToken = default);
}

public interface ISubscriptionStateMachine
{
    void Transition(Subscription subscription, CoachPulse.Domain.Enums.SubscriptionStatus next, string? reason = null);
}
