using System.Collections.Concurrent;
using System.Security.Claims;
using CoachPulse.Api.Services;
using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Hubs;

[Authorize]
public sealed class ChatHub(CoachPulseDbContext db, ChatMessageService messageService, IHubContext<NotificationsHub> notifications, INotificationService notificationService, ILogger<ChatHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<Guid,int> Connections = new();
    private static readonly ConcurrentDictionary<string,(DateTime Last,Guid Partner)> TypingWindows = new();
    private static readonly ConcurrentDictionary<string,Guid[]> ConnectionPartners = new();

    public override async Task OnConnectedAsync()
    {
        if (TryIdentity(out var user,out var tenant,out var role))
        {
            var partners = role == "Client" ? await db.Clients.IgnoreQueryFilters().Where(c=>c.UserId==user&&c.TenantId==tenant&&c.CoachId!=null).Select(c=>c.CoachId!.Value).ToArrayAsync() : role == "TenantOwner" ? await db.Clients.IgnoreQueryFilters().Where(c=>c.TenantId==tenant).Select(c=>c.UserId).ToArrayAsync() : await db.Clients.IgnoreQueryFilters().Where(c=>c.CoachId==user&&c.TenantId==tenant).Select(c=>c.UserId).ToArrayAsync();
            logger.LogInformation("Chat hub connected for role {Role}; partner count {PartnerCount}", role, partners.Length);
            ConnectionPartners[Context.ConnectionId]=partners;
            var count=Connections.AddOrUpdate(user,1,(_,n)=>n+1);
            if(count==1) foreach(var partner in partners) await Clients.User(partner.ToString()).SendAsync("PresenceChanged",new{userId=user,isOnline=true});
            // Tell this connection the current state of its chat partners so the UI never stays "unknown".
            foreach(var partner in partners) await Clients.Caller.SendAsync("PresenceChanged",new{userId=partner,isOnline=Connections.ContainsKey(partner)});
        }
        else logger.LogWarning("Chat hub connection has no valid user and tenant claims");
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if(TryIdentity(out var user,out _,out _))
        {
            ConnectionPartners.TryRemove(Context.ConnectionId,out var partners);
            var remaining=Connections.AddOrUpdate(user,0,(_,n)=>Math.Max(0,n-1));
            if(remaining==0){Connections.TryRemove(user,out _);if(partners is not null)foreach(var partner in partners)await Clients.User(partner.ToString()).SendAsync("PresenceChanged",new{userId=user,isOnline=false});}
        }
        await base.OnDisconnectedAsync(exception);
    }
    public async Task JoinConversation(Guid otherUserId)
    {
        if(!TryIdentity(out var user,out var tenant,out var role)){logger.LogWarning("Chat hub rejected conversation join: invalid identity");throw new HubException("Conversation not allowed.");}
        if(!await CanChat(user,otherUserId,tenant)){logger.LogWarning("Chat hub rejected conversation join for role {Role}",role);throw new HubException("Conversation not allowed.");}
        await Groups.AddToGroupAsync(Context.ConnectionId,ChatMessageService.Group(user,otherUserId));
        logger.LogInformation("Chat hub conversation joined for role {Role}",role);
    }
    public async Task SendMessage(Guid receiverId,string content,IReadOnlyCollection<Guid>? fileIds=null)
    {
        if(!TryIdentity(out var user,out var tenant,out var role)||!await CanChat(user,receiverId,tenant))throw new HubException("Conversation not allowed.");
        try{var clientUser=role=="Client"?user:receiverId;var msg=await messageService.SendAsync(user,tenant,role,clientUser,content,fileIds,CancellationToken.None);if(msg is null)throw new HubException("Conversation not allowed.");var attached=await db.StoredFiles.IgnoreQueryFilters().Where(f=>f.TenantId==tenant&&f.LinkedEntityType=="Message"&&f.LinkedEntityId==msg.Id&&!f.IsDeleted).Select(f=>f.Id).ToArrayAsync();await Clients.Group(ChatMessageService.Group(msg.SenderId,msg.ReceiverId)).SendAsync("ReceiveMessage",new{msg.Id,msg.SenderId,msg.ReceiverId,msg.Content,msg.Timestamp,fileIds=attached});await notificationService.NotifyAsync(msg.ReceiverId,tenant,"MessageReceived","رسالة جديدة","لديك رسالة جديدة.",System.Text.Json.JsonSerializer.Serialize(new{messageId=msg.Id,conversationId=clientUser}),CancellationToken.None);var count=await db.Messages.IgnoreQueryFilters().CountAsync(m=>m.TenantId==tenant&&m.ReceiverId==msg.ReceiverId&&!m.IsRead&&!m.IsDeleted);await notifications.Clients.User(msg.ReceiverId.ToString()).SendAsync("UnreadCountUpdated",new{count});}
        catch(ArgumentException ex){throw new HubException(ex.Message);}catch(InvalidOperationException ex){throw new HubException(ex.Message);}
    }
    public async Task MarkAsRead(Guid conversationId,Guid lastMessageId)
    {
        if(!TryIdentity(out var user,out var tenant,out _)||!await CanChat(user,conversationId,tenant))throw new HubException("Conversation not allowed.");
        var last=await db.Messages.IgnoreQueryFilters().FirstOrDefaultAsync(m=>m.Id==lastMessageId&&m.TenantId==tenant&&((m.SenderId==user&&m.ReceiverId==conversationId)||(m.SenderId==conversationId&&m.ReceiverId==user)));
        if(last is null)throw new HubException("Message does not belong to conversation.");
        var unread=await db.Messages.IgnoreQueryFilters().Where(m=>m.TenantId==tenant&&m.SenderId==conversationId&&m.ReceiverId==user&&!m.IsRead&&m.Timestamp<=last.Timestamp).ToListAsync();foreach(var m in unread){m.IsRead=true;m.ReadAt=DateTime.UtcNow;}await db.SaveChangesAsync();
        await Clients.Group(ChatMessageService.Group(user,conversationId)).SendAsync("MessageRead",new{conversationId,lastMessageId,readerId=user});var count=await db.Messages.IgnoreQueryFilters().CountAsync(m=>m.TenantId==tenant&&m.ReceiverId==user&&!m.IsRead&&!m.IsDeleted);await Clients.User(user.ToString()).SendAsync("UnreadCountUpdated",new{count});
    }
    public async Task Typing(Guid conversationId)
    {
        if(!TryIdentity(out var user,out var tenant,out _)||!await CanChat(user,conversationId,tenant))throw new HubException("Conversation not allowed.");var key=$"{user:N}:{conversationId:N}";var now=DateTime.UtcNow;if(TypingWindows.TryGetValue(key,out var old)&&now-old.Last<TimeSpan.FromSeconds(2))return;TypingWindows[key]=(now,conversationId);await Clients.User(conversationId.ToString()).SendAsync("UserTyping",new{userId=user,conversationId});
    }
    private async Task<bool> CanChat(Guid user,Guid other,Guid tenant){var role=Context.User?.FindFirst(ClaimTypes.Role)?.Value;return role=="Client"?await db.Clients.IgnoreQueryFilters().AnyAsync(c=>c.UserId==user&&c.CoachId==other&&c.TenantId==tenant):role=="TenantOwner"?await db.Clients.IgnoreQueryFilters().AnyAsync(c=>c.UserId==other&&c.TenantId==tenant):role=="CoachStaff"&&await db.Clients.IgnoreQueryFilters().AnyAsync(c=>c.UserId==other&&c.CoachId==user&&c.TenantId==tenant);}
    private bool TryIdentity(out Guid user,out Guid tenant,out string role){role=Context.User?.FindFirst(ClaimTypes.Role)?.Value??"";user=Guid.Empty;tenant=Guid.Empty;return Guid.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,out user)&&Guid.TryParse(Context.User?.FindFirst("tenantId")?.Value,out tenant);}
}
