using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CoachPulse.Api.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
}
