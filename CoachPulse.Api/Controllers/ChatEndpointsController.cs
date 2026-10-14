using System.Security.Claims;
using CoachPulse.Api.Hubs;
using CoachPulse.Api.Services;
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
    private async Task<bool> CanAccessConversation(Guid me, Guid tenant, string role, Guid clientUser) { var client=role=="Client"; return await db.Clients.AnyAsync(c => c.TenantId == tenant && c.UserId == clientUser && (role=="Owner" || (client ? c.UserId == me : c.CoachId == me))); }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations([FromQuery] int page = 1, [FromQuery] int limit = 25, CancellationToken ct = default)
    {
        if (!Identity(out var me, out var tenant, out var role)) return Unauthorized(); if (page < 1 || limit is < 1 or > 100) return UnprocessableEntity();
        var clients = role == "Client"
            ? await db.Clients.AsNoTracking().Where(c=>c.TenantId==tenant&&c.UserId==me&&c.CoachId!=null).Select(c=>new { UserId=c.CoachId!.Value, Name=c.Coach!.Name??c.Coach.Email, CoachId=(Guid?)null }).ToListAsync(ct)
            : await db.Clients.AsNoTracking().Where(c=>c.TenantId==tenant&&(role=="Owner"||c.CoachId==me)).Select(c=>new { c.UserId, Name=c.User.Name??c.User.Email, CoachId=c.CoachId }).ToListAsync(ct);
        var ids = clients.Select(c => c.UserId).ToArray();
        var all = await db.Messages.AsNoTracking().Where(m => m.TenantId == tenant && (role=="Owner" ? ids.Contains(m.SenderId)||ids.Contains(m.ReceiverId) : m.SenderId == me && ids.Contains(m.ReceiverId) || ids.Contains(m.SenderId) && m.ReceiverId == me)).OrderByDescending(m => m.Timestamp).ToListAsync(ct);
        var rows = clients.Select(c => { var conversation = all.Where(m => (m.SenderId == me && m.ReceiverId == c.UserId) || (m.SenderId == c.UserId && m.ReceiverId == me) || (role=="Owner"&&c.CoachId.HasValue&&((m.SenderId==c.UserId&&m.ReceiverId==c.CoachId)||(m.SenderId==c.CoachId&&m.ReceiverId==c.UserId)))).ToArray(); return new { clientId = c.UserId, c.Name, unreadCount = conversation.Count(m => (role=="Owner"||m.ReceiverId == me) && !m.IsRead), lastMessage = conversation.FirstOrDefault() is { } m ? new { m.Id, content = m.IsDeleted ? "تم حذف الرسالة" : m.Content, m.SenderId, m.Timestamp } : null }; }).OrderByDescending(x => x.lastMessage?.Timestamp).Skip((page-1)*limit).Take(limit).ToArray();
        return Ok(new { page, limit, total = clients.Count, items = rows });
    }

    [HttpPost("messages/{clientId:guid}")]
    [Authorize(Roles = "Owner,Staff")]
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
        try { var msg = await messages.SendAsync(me, tenant, role, clientMode ? me : recipient, req.Content, req.FileIds, ct); if (msg is null) return NotFound(); var fileIds=await db.StoredFiles.Where(f=>f.LinkedEntityType=="Message"&&f.LinkedEntityId==msg.Id&&!f.IsDeleted).Select(f=>f.Id).ToArrayAsync(ct); var payload = new { msg.Id, msg.SenderId, msg.ReceiverId, msg.Content, msg.Timestamp, fileIds }; await chatHub.Clients.Group(ChatMessageService.Group(msg.SenderId, msg.ReceiverId)).SendAsync("ReceiveMessage", payload, ct); await HttpContext.RequestServices.GetRequiredService<INotificationService>().NotifyAsync(msg.ReceiverId, tenant, "MessageReceived", "رسالة جديدة", "لديك رسالة جديدة.", System.Text.Json.JsonSerializer.Serialize(new { messageId = msg.Id, conversationId = clientMode ? me : recipient }), ct); await SendUnread(msg.ReceiverId, tenant, ct); return StatusCode(201, payload); }
        catch (KeyNotFoundException) { return NotFound(); } catch (ArgumentException e) { return UnprocessableEntity(e.Message); } catch (InvalidOperationException) { return StatusCode(429); }
    }

    [HttpGet("my/messages")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> MyMessages([FromQuery] Guid? before = null, [FromQuery] int limit = 50, CancellationToken ct = default)
    { if (!Identity(out var me,out _,out _)) return Unauthorized(); if (limit is < 1 or > 100) return UnprocessableEntity(); var coach=await db.Clients.Where(c=>c.UserId==me).Select(c=>c.CoachId).FirstOrDefaultAsync(ct); if(coach is null)return Ok(Array.Empty<object>()); return await History(coach.Value,before,limit,ct); }
    [NonAction]
    public async Task<IActionResult> History(Guid clientId,[FromQuery] Guid? before = null,[FromQuery] int limit=50,CancellationToken ct=default)
    { if (!Identity(out var me,out var tenant,out var role)) return Unauthorized(); if(limit is <1 or >100)return UnprocessableEntity(); if(!await CanAccessConversation(me,tenant,role,clientId))return NotFound(); var first=me;var second=clientId;if(role=="Owner"){var coach=await db.Clients.Where(c=>c.TenantId==tenant&&c.UserId==clientId).Select(c=>c.CoachId).FirstOrDefaultAsync(ct);if(coach is null)return NotFound();first=clientId;second=coach.Value;db.SecurityAuditLogs.Add(new(){Id=Guid.NewGuid(),UserId=me,TenantId=tenant,EventType="chat.conversation.read",Identifier=$"client={clientId}",CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync(ct);} var q=db.Messages.AsNoTracking().Where(m=>m.TenantId==tenant&&((m.SenderId==first&&m.ReceiverId==second)||(m.SenderId==second&&m.ReceiverId==first))); if(before.HasValue){var timestamp=await db.Messages.Where(m=>m.Id==before&&m.TenantId==tenant&&((m.SenderId==first&&m.ReceiverId==second)||(m.SenderId==second&&m.ReceiverId==first))).Select(m=>(DateTime?)m.Timestamp).FirstOrDefaultAsync(ct);if(timestamp is null)return UnprocessableEntity("before must belong to this conversation.");q=q.Where(m=>m.Timestamp<timestamp);} var rows=await q.OrderByDescending(m=>m.Timestamp).Take(limit).Select(m=>new{m.Id,m.SenderId,m.ReceiverId,content=m.IsDeleted?"تم حذف الرسالة":m.Content,m.Timestamp,m.IsRead,fileIds=m.IsDeleted?Array.Empty<Guid>():db.StoredFiles.Where(f=>f.LinkedEntityType=="Message"&&f.LinkedEntityId==m.Id&&!f.IsDeleted).Select(f=>f.Id).ToArray()}).ToListAsync(ct); rows.Reverse(); return Ok(rows); }

    [HttpPut("messages/{clientId:guid}/read")]
    [Authorize(Roles="Owner,Staff")]
    public Task<IActionResult> ReadCoach(Guid clientId,[FromBody] ReadMessageRequest req,CancellationToken ct)=>MarkRead(clientId,req,ct);
    [HttpPut("my/messages/read")]
    [Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> ReadClient([FromBody] ReadMessageRequest req,CancellationToken ct){if(!Identity(out var me,out _,out _))return Unauthorized();var coach=await db.Clients.Where(c=>c.UserId==me).Select(c=>c.CoachId).FirstOrDefaultAsync(ct);if(coach is null)return NotFound();return await MarkRead(coach.Value,req,ct,true);}
    private async Task<IActionResult> MarkRead(Guid clientUser,ReadMessageRequest req,CancellationToken ct,bool clientMode=false){if(!Identity(out var me,out var tenant,out var role))return Unauthorized();if(role=="Owner")return Forbid();if(!await CanAccessConversation(me,tenant,role,clientUser))return NotFound();var last=await db.Messages.FirstOrDefaultAsync(m=>m.Id==req.LastMessageId&&m.TenantId==tenant&&((m.SenderId==me&&m.ReceiverId==clientUser)||(m.SenderId==clientUser&&m.ReceiverId==me)),ct);if(last is null)return UnprocessableEntity("messageId must belong to this conversation.");var through=last.Timestamp;var changed=await db.Messages.Where(m=>m.TenantId==tenant&&m.SenderId==clientUser&&m.ReceiverId==me&&!m.IsRead&&m.Timestamp<=through).ToListAsync(ct);foreach(var m in changed){m.IsRead=true;m.ReadAt=DateTime.UtcNow;}await db.SaveChangesAsync(ct);await chatHub.Clients.Group(ChatMessageService.Group(me,clientUser)).SendAsync("MessageRead",new{conversationId=clientUser,lastMessageId=last.Id,readerId=me},ct);await SendUnread(me,tenant,ct);return NoContent();}

    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid messageId,CancellationToken ct){if(!Identity(out var me,out var tenant,out _))return Unauthorized();var m=await db.Messages.FirstOrDefaultAsync(x=>x.Id==messageId&&x.TenantId==tenant&&x.SenderId==me,ct);if(m is null)return NotFound();m.IsDeleted=true;m.Content="";await db.SaveChangesAsync(ct);await chatHub.Clients.Group(ChatMessageService.Group(m.SenderId,m.ReceiverId)).SendAsync("ReceiveMessage",new{messageId=m.Id,senderId=m.SenderId,receiverId=m.ReceiverId,content="تم حذف الرسالة",deleted=true},ct);await SendUnread(m.ReceiverId,tenant,ct);return NoContent();}
    [HttpGet("messages/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct){if(!Identity(out var me,out var tenant,out _))return Unauthorized();return Ok(new{count=await db.Messages.CountAsync(m=>m.TenantId==tenant&&m.ReceiverId==me&&!m.IsRead&&!m.IsDeleted,ct)});}
    private async Task SendUnread(Guid user,Guid tenant,CancellationToken ct){var count=await db.Messages.CountAsync(m=>m.TenantId==tenant&&m.ReceiverId==user&&!m.IsRead&&!m.IsDeleted,ct);await notifications.Clients.User(user.ToString()).SendAsync("UnreadCountUpdated",new{count},ct);}
}
public sealed record SendMessageRequest(string? Content, List<Guid>? FileIds);
public sealed record ReadMessageRequest(Guid LastMessageId);
