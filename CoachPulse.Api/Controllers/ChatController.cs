
using System.Security.Claims;
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

    // GET /api/chat/conversations/{otherUserId}/messages?page=1&pageSize=50
    [HttpGet("conversations/{otherUserId:guid}/messages")]
    [Obsolete("Use GET /api/messages/{clientId}.")]
    public async Task<IActionResult> GetMessages(
        Guid otherUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (Request.Path.StartsWithSegments("/api/chat/conversations")) RouteDeprecation.AddHeaders(Response, $"/api/messages/{otherUserId}");
        return await GetMessagesCore(otherUserId, page, pageSize);
    }

    [HttpGet("~/api/messages/{clientId:guid}")]
    public Task<IActionResult> GetMessagesCanonical(Guid clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) => GetMessagesCore(clientId, page, pageSize);

    private async Task<IActionResult> GetMessagesCore(Guid otherUserId, int page, int pageSize)
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
        else if (role is "Owner" or "Staff")
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
            return Forbid();
        }

        if (!canChat)
        {
            return Forbid();
        }

        var query = _context.Messages
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                (
                    (m.SenderId == userId &&
                     m.ReceiverId == otherUserId)
                    ||
                    (m.SenderId == otherUserId &&
                     m.ReceiverId == userId)
                ));

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
                m.Content,
                m.Timestamp
            })
            .ToListAsync();

        // Return messages in chronological order.
        messages.Reverse();

        return Ok(new
        {
            otherUserId,
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),
            messages
        });
    }
}
