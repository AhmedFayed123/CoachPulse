using System.Security.Claims;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly CoachPulseDbContext _context;

    public ChatHub(CoachPulseDbContext context)
    {
        _context = context;
    }

    public async Task JoinConversation(Guid otherUserId)
    {
        if (!TryGetIdentity(out var userId, out var tenantId))
            throw new HubException("Unauthorized.");

        if (otherUserId == userId)
            throw new HubException("You cannot chat with yourself.");

        var canChat = await CanChatAsync(
            userId,
            otherUserId,
            tenantId);

        if (!canChat)
            throw new HubException("Conversation not allowed.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetConversationGroup(userId, otherUserId));
    }

    public async Task SendMessage(
        Guid receiverId,
        string content)
    {
        if (!TryGetIdentity(out var senderId, out var tenantId))
            throw new HubException("Unauthorized.");

        if (receiverId == senderId)
            throw new HubException("You cannot message yourself.");

        if (string.IsNullOrWhiteSpace(content))
            throw new HubException("Message cannot be empty.");

        content = content.Trim();

        if (content.Length > 2000)
            throw new HubException(
                "Message cannot exceed 2000 characters.");

        var canChat = await CanChatAsync(
            senderId,
            receiverId,
            tenantId);

        if (!canChat)
            throw new HubException("Conversation not allowed.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        _context.Messages.Add(message);

        await _context.SaveChangesAsync();

        var response = new
        {
            message.Id,
            message.TenantId,
            message.SenderId,
            message.ReceiverId,
            message.Content,
            message.Timestamp
        };

        await Clients.Group(
            GetConversationGroup(senderId, receiverId))
            .SendAsync("ReceiveMessage", response);
    }

    private async Task<bool> CanChatAsync(
        Guid userId,
        Guid otherUserId,
        Guid tenantId)
    {
        var role = Context.User?.FindFirst(
            ClaimTypes.Role)?.Value;

        // A client can chat with their assigned coach only.
        if (role == "Client")
        {
            return await _context.Clients
                .AsNoTracking()
                .AnyAsync(c =>
                    c.UserId == userId &&
                    c.CoachId == otherUserId &&
                    c.TenantId == tenantId);
        }

        // A coach can chat with their assigned clients only.
        if (role is "Owner" or "Staff")
        {
            return await _context.Clients
                .AsNoTracking()
                .AnyAsync(c =>
                    c.UserId == otherUserId &&
                    c.CoachId == userId &&
                    c.TenantId == tenantId);
        }

        return false;
    }

    private bool TryGetIdentity(
        out Guid userId,
        out Guid tenantId)
    {
        userId = Guid.Empty;
        tenantId = Guid.Empty;

        var userIdValue = Context.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = Context.User?
            .FindFirst("tenantId")?.Value;

        return Guid.TryParse(userIdValue, out userId)
            && Guid.TryParse(tenantIdValue, out tenantId);
    }

    private static string GetConversationGroup(
        Guid firstUserId,
        Guid secondUserId)
    {
        var ids = new[]
        {
            firstUserId.ToString("N"),
            secondUserId.ToString("N")
        };

        Array.Sort(ids, StringComparer.Ordinal);

        return $"conversation:{ids[0]}:{ids[1]}";
    }
}
