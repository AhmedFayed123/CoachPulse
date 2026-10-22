
using System.Security.Claims;
using CoachPulse.Application.DTOs;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly CoachPulseDbContext _context;

    public ChatController(CoachPulseDbContext context)
    {
        _context = context;
    }

    // GET /api/chat/conversations/{otherUserId}/messages?page=1&pageSize=20
    [HttpGet("conversations/{otherUserId:guid}/messages")]
    [Obsolete("Use GET /api/messages/{clientId}.")]
    public async Task<IActionResult> GetMessages(
        Guid otherUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? before = null,
        [FromQuery] int? limit = null)
    {
        if (Request.Path.StartsWithSegments("/api/chat/conversations")) RouteDeprecation.AddHeaders(Response, $"/api/messages/{otherUserId}");
        return await GetMessagesCore(otherUserId, page, pageSize, before, limit);
    }

    [HttpGet("~/api/messages/{clientId:guid}")]
    public Task<IActionResult> GetMessagesCanonical(Guid clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? before = null, [FromQuery] int? limit = null) => GetMessagesCore(clientId, page, pageSize, before, limit);

    private async Task<IActionResult> GetMessagesCore(Guid otherUserId, int page, int pageSize, Guid? before = null, int? limit = null)
    {
        var userIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue = User.FindFirst("tenantId")?.Value;

        var role = User.FindFirst(
            ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Unauthorized();
        }

        if (otherUserId == userId)
        {
            return BadRequest(new
            {
                message = "You cannot open a conversation with yourself."
            });
        }

        if (page < 1)
        {
            return BadRequest(new
            {
                message = "Page must be greater than zero."
            });
        }

        pageSize = limit ?? pageSize;
        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "PageSize must be between 1 and 100."
            });
        }

        // Verify that these users are allowed to chat.
        bool canChat;

        if (role == "Client")
        {
            canChat = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c =>
                    c.UserId == userId &&
                    c.CoachId == otherUserId &&
                    c.TenantId == tenantId);
        }
        else if (role == "TenantOwner")
        {
            canChat = await _context.Clients.AsNoTracking().AnyAsync(c => c.UserId == otherUserId && c.TenantId == tenantId);
        }
        else if (role == "CoachStaff")
        {
            canChat = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c =>
                    c.UserId == otherUserId &&
                    c.CoachId == userId &&
                    c.TenantId == tenantId);
        }
        else
        {
            return NotFound();
        }

        if (!canChat)
        {
            return NotFound();
        }

        var firstParticipant = userId;
        var secondParticipant = otherUserId;
        if (role == "TenantOwner")
        {
            var hasOwnerConversation = await _context.Messages.AnyAsync(m => m.TenantId == tenantId && ((m.SenderId == userId && m.ReceiverId == otherUserId) || (m.SenderId == otherUserId && m.ReceiverId == userId)));
            if (!hasOwnerConversation)
            {
                var assignedCoach = await _context.Clients.Where(c => c.TenantId == tenantId && c.UserId == otherUserId).Select(c => c.CoachId).FirstOrDefaultAsync();
                if (assignedCoach is not null)
                {
                    firstParticipant = otherUserId;
                    secondParticipant = assignedCoach.Value;
                }
            }
        }
        var query = _context.Messages
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                (
                    (m.SenderId == firstParticipant &&
                     m.ReceiverId == secondParticipant)
                    ||
                    (m.SenderId == secondParticipant &&
                     m.ReceiverId == firstParticipant)
                ));

        if (role == "TenantOwner") { _context.SecurityAuditLogs.Add(new() { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, EventType = "chat.conversation.read", Identifier = $"client={otherUserId}", CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); }
        if (before.HasValue) { var cursor = await _context.Messages.Where(m => m.Id == before && m.TenantId == tenantId && ((m.SenderId == firstParticipant && m.ReceiverId == secondParticipant) || (m.SenderId == secondParticipant && m.ReceiverId == firstParticipant))).Select(m => (DateTime?)m.Timestamp).FirstOrDefaultAsync(); if (cursor is null) return UnprocessableEntity("before must belong to this conversation."); query = query.Where(m => m.Timestamp < cursor); }
        var totalCount = await query.CountAsync();

        // Retrieve the newest page first.
        var messages = await query
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.SenderId,
                m.ReceiverId,
                Content = m.IsDeleted ? "تم حذف الرسالة" : m.Content,
                m.Timestamp,
                FileIds = m.IsDeleted ? Array.Empty<Guid>() : _context.StoredFiles.Where(f => f.LinkedEntityType == "Message" && f.LinkedEntityId == m.Id && !f.IsDeleted).Select(f => f.Id).ToArray()
            })
            .ToListAsync();

        // Return messages in chronological order.
        messages.Reverse();

        return Ok(PagedResult<object>.Create(messages.Cast<object>().ToArray(), page, pageSize, totalCount));
    }
}
