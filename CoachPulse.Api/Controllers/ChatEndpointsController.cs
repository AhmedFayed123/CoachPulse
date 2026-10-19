using System.Security.Claims;
using CoachPulse.Api.Hubs;
using CoachPulse.Api.Services;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Route("api"), Authorize]
public sealed class ChatEndpointsController(CoachPulseDbContext db, ChatMessageService messages, IHubContext<ChatHub> chatHub, IHubContext<NotificationsHub> notifications) : ControllerBase
{
    private bool Identity(out Guid user, out Guid tenant, out string role) { role = User.FindFirst(ClaimTypes.Role)?.Value ?? ""; user=Guid.Empty;tenant=Guid.Empty; return Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out user) && Guid.TryParse(User.FindFirst("tenantId")?.Value, out tenant); }
    private async Task<bool> CanAccessConversation(Guid me, Guid tenant, string role, Guid otherUser)
    {
        return role switch
        {
            // Client routes pass the assigned coach's user ID; coach routes pass the client's user ID.
            "Client" => await db.Clients.AnyAsync(c => c.TenantId == tenant && c.UserId == me && c.CoachId == otherUser),
            "TenantOwner" => await db.Clients.AnyAsync(c => c.TenantId == tenant && c.UserId == otherUser),
            "CoachStaff" => await db.Clients.AnyAsync(c => c.TenantId == tenant && c.UserId == otherUser && c.CoachId == me),
            _ => false
        };
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(PagedResult<ConversationListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Conversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!Identity(out var me, out var tenant, out var role)) return Unauthorized(); if (page < 1 || pageSize is < 1 or > 100) return UnprocessableEntity("page must be at least 1 and pageSize must be between 1 and 100.");
        var clients = role == "Client"
            ? await db.Clients.AsNoTracking().Where(c=>c.TenantId==tenant&&c.UserId==me&&c.CoachId!=null).Select(c=>new ConversationParticipant(c.CoachId!.Value, c.Coach!.Name??c.Coach.Email, null)).ToListAsync(ct)
            : await db.Clients.AsNoTracking().Where(c=>c.TenantId==tenant&&(role=="TenantOwner"||c.CoachId==me)).Select(c=>new ConversationParticipant(c.UserId, c.User.Name??c.User.Email, c.CoachId)).ToListAsync(ct);
        var totalCount = clients.Count;
        clients = clients.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var ids = clients.Select(c => c.UserId).ToArray();
        var all = await db.Messages.AsNoTracking().Where(m => m.TenantId == tenant && (role=="TenantOwner" ? ids.Contains(m.SenderId)||ids.Contains(m.ReceiverId) : m.SenderId == me && ids.Contains(m.ReceiverId) || ids.Contains(m.SenderId) && m.ReceiverId == me)).OrderByDescending(m => m.Timestamp).ToListAsync(ct);
        var rows = clients.Select(c =>
        {
            var conversation = all.Where(m => (m.SenderId == me && m.ReceiverId == c.UserId) || (m.SenderId == c.UserId && m.ReceiverId == me) ||
                (role == "TenantOwner" && c.CoachId.HasValue && ((m.SenderId == c.UserId && m.ReceiverId == c.CoachId) || (m.SenderId == c.CoachId && m.ReceiverId == c.UserId)))).ToArray();
            var last = conversation.FirstOrDefault();
            return new ConversationListItem(c.UserId, c.Name,
                conversation.Count(m => (role == "TenantOwner" || m.ReceiverId == me) && !m.IsRead),
                last is null ? null : new ConversationLastMessage(last.Id, last.IsDeleted ? "طھظ… ط­ط°ظپ ط§ظ„ط±ط³ط§ظ„ط©" : last.Content, last.SenderId, last.Timestamp));
        }).OrderByDescending(x => x.LastMessage?.Timestamp).ToArray();
        return Ok(PagedResult<ConversationListItem>.Create(rows, page, pageSize, totalCount));
    }

    [HttpPost("messages/{clientId:guid}")]
    [Authorize(Roles = "TenantOwner,CoachStaff")]
    public Task<IActionResult> SendCoach(Guid clientId, [FromBody] SendMessageRequest req, CancellationToken ct) => Send(clientId, req, ct);
    [HttpPost("my/messages")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> SendClient([FromBody] SendMessageRequest req, CancellationToken ct)
    {
        if (!Identity(out var me, out _, out _)) return Unauthorized(); var coach = await db.Clients.Where(c => c.UserId == me).Select(c => c.CoachId).FirstOrDefaultAsync(ct); if (coach is null) return Conflict("No assigned coach."); return await Send(coach.Value, req, ct, true);
    }
    private async Task<IActionResult> Send(Guid recipient, SendMessageRequest req, CancellationToken ct, bool clientMode = false)
    {
        if (!Identity(out var me, out var tenant, out var role)) return Unauthorized();
        try { var msg = await messages.SendAsync(me, tenant, role, clientMode ? me : recipient, req.Content, req.FileIds, ct); if (msg is null) return NotFound(); var fileIds=await db.StoredFiles.Where(f=>f.LinkedEntityType=="Message"&&f.LinkedEntityId==msg.Id&&!f.IsDeleted).Select(f=>f.Id).ToArrayAsync(ct); var payload = new { msg.Id, msg.SenderId, msg.ReceiverId, msg.Content, msg.Timestamp, fileIds }; await chatHub.Clients.Group(ChatMessageService.Group(msg.SenderId, msg.ReceiverId)).SendAsync("ReceiveMessage", payload, ct); await HttpContext.RequestServices.GetRequiredService<INotificationService>().NotifyAsync(msg.ReceiverId, tenant, "MessageReceived", "\u0631\u0633\u0627\u0644\u0629 \u062c\u062f\u064a\u062f\u0629", "\u0644\u062f\u064a\u0643 \u0631\u0633\u0627\u0644\u0629 \u062c\u062f\u064a\u062f\u0629.", System.Text.Json.JsonSerializer.Serialize(new { messageId = msg.Id, conversationId = clientMode ? me : recipient }), ct); await SendUnread(msg.ReceiverId, tenant, ct); return StatusCode(201, payload); }
        catch (KeyNotFoundException) { return NotFound(); } catch (ArgumentException e) { return UnprocessableEntity(e.Message); } catch (InvalidOperationException) { return StatusCode(429); }
    }

    [HttpGet("my/messages")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> MyMessages([FromQuery] Guid? before = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? limit = null, CancellationToken ct = default)
    {
        if (!Identity(out var me, out _, out _)) return Unauthorized();
        pageSize = limit ?? pageSize;
        if (page < 1 || pageSize is < 1 or > 100) return UnprocessableEntity();
        var coach = await db.Clients.Where(c => c.UserId == me).Select(c => c.CoachId).FirstOrDefaultAsync(ct);
        if (coach is null) return Ok(PagedResult<object>.Create(Array.Empty<object>(), page, pageSize, 0));
        return await History(coach.Value, before, page, pageSize, ct);
    }

    [NonAction]
    public async Task<IActionResult> History(Guid clientId, Guid? before = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (!Identity(out var me, out var tenant, out var role)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return UnprocessableEntity();
        if (!await CanAccessConversation(me, tenant, role, clientId)) return NotFound();
        var first = me; var second = clientId;
        if (role == "TenantOwner")
        {
            var coach = await db.Clients.Where(c => c.TenantId == tenant && c.UserId == clientId).Select(c => c.CoachId).FirstOrDefaultAsync(ct);
            if (coach is null) return NotFound();
            first = clientId; second = coach.Value;
            db.SecurityAuditLogs.Add(new() { Id = Guid.NewGuid(), UserId = me, TenantId = tenant, EventType = "chat.conversation.read", Identifier = $"client={clientId}", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }
        var query = db.Messages.AsNoTracking().Where(m => m.TenantId == tenant && ((m.SenderId == first && m.ReceiverId == second) || (m.SenderId == second && m.ReceiverId == first)));
        if (before.HasValue)
        {
            var timestamp = await query.Where(m => m.Id == before.Value).Select(m => (DateTime?)m.Timestamp).FirstOrDefaultAsync(ct);
            if (timestamp is null) return UnprocessableEntity("before must belong to this conversation.");
            query = query.Where(m => m.Timestamp < timestamp.Value);
        }
        var totalCount = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(m => m.Timestamp).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new { m.Id, m.SenderId, m.ReceiverId, content = m.IsDeleted ? "تم حذف الرسالة" : m.Content, m.Timestamp, m.IsRead,
                fileIds = m.IsDeleted ? Array.Empty<Guid>() : db.StoredFiles.Where(f => f.LinkedEntityType == "Message" && f.LinkedEntityId == m.Id && !f.IsDeleted).Select(f => f.Id).ToArray() })
            .ToListAsync(ct);
        rows.Reverse();
        return Ok(PagedResult<object>.Create(rows.Cast<object>().ToArray(), page, pageSize, totalCount));
    }

    [HttpPut("messages/{clientId:guid}/read")]
    [Authorize(Roles="TenantOwner,CoachStaff")]
    public Task<IActionResult> ReadCoach(Guid clientId,[FromBody] ReadMessageRequest req,CancellationToken ct)=>MarkRead(clientId,req,ct);
    [HttpPut("my/messages/read")]
    [Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> ReadClient([FromBody] ReadMessageRequest req,CancellationToken ct){if(!Identity(out var me,out _,out _))return Unauthorized();var coach=await db.Clients.Where(c=>c.UserId==me).Select(c=>c.CoachId).FirstOrDefaultAsync(ct);if(coach is null)return NotFound();return await MarkRead(coach.Value,req,ct,true);}
    private async Task<IActionResult> MarkRead(Guid clientUser,ReadMessageRequest req,CancellationToken ct,bool clientMode=false){if(!Identity(out var me,out var tenant,out var role))return Unauthorized();if(role=="TenantOwner")return Forbid();if(!await CanAccessConversation(me,tenant,role,clientUser))return NotFound();var last=await db.Messages.FirstOrDefaultAsync(m=>m.Id==req.LastMessageId&&m.TenantId==tenant&&((m.SenderId==me&&m.ReceiverId==clientUser)||(m.SenderId==clientUser&&m.ReceiverId==me)),ct);if(last is null)return UnprocessableEntity("messageId must belong to this conversation.");var through=last.Timestamp;var changed=await db.Messages.Where(m=>m.TenantId==tenant&&m.SenderId==clientUser&&m.ReceiverId==me&&!m.IsRead&&m.Timestamp<=through).ToListAsync(ct);foreach(var m in changed){m.IsRead=true;m.ReadAt=DateTime.UtcNow;}await db.SaveChangesAsync(ct);await chatHub.Clients.Group(ChatMessageService.Group(me,clientUser)).SendAsync("MessageRead",new{conversationId=clientUser,lastMessageId=last.Id,readerId=me},ct);await SendUnread(me,tenant,ct);return NoContent();}

    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid messageId,CancellationToken ct){if(!Identity(out var me,out var tenant,out _))return Unauthorized();var m=await db.Messages.FirstOrDefaultAsync(x=>x.Id==messageId&&x.TenantId==tenant&&x.SenderId==me,ct);if(m is null)return NotFound();m.IsDeleted=true;m.Content="";await db.SaveChangesAsync(ct);await chatHub.Clients.Group(ChatMessageService.Group(m.SenderId,m.ReceiverId)).SendAsync("ReceiveMessage",new{messageId=m.Id,senderId=m.SenderId,receiverId=m.ReceiverId,content="ط·ع¾ط¸â€¦ ط·آ­ط·آ°ط¸ظ¾ ط·آ§ط¸â€‍ط·آ±ط·آ³ط·آ§ط¸â€‍ط·آ©",deleted=true},ct);await SendUnread(m.ReceiverId,tenant,ct);return NoContent();}
    [HttpGet("messages/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct){if(!Identity(out var me,out var tenant,out _))return Unauthorized();return Ok(new{count=await db.Messages.CountAsync(m=>m.TenantId==tenant&&m.ReceiverId==me&&!m.IsRead&&!m.IsDeleted,ct)});}
    private async Task SendUnread(Guid user,Guid tenant,CancellationToken ct){var count=await db.Messages.CountAsync(m=>m.TenantId==tenant&&m.ReceiverId==user&&!m.IsRead&&!m.IsDeleted,ct);await notifications.Clients.User(user.ToString()).SendAsync("UnreadCountUpdated",new{count},ct);}
    private sealed record ConversationParticipant(Guid UserId, string Name, Guid? CoachId);
}
public sealed record SendMessageRequest(string? Content, List<Guid>? FileIds);
public sealed record ReadMessageRequest(Guid LastMessageId);
