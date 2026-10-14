namespace CoachPulse.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, Guid tenantId, string type, string title, string body, string? dataJson = null, CancellationToken cancellationToken = default);
}
