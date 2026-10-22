using System.Collections.Concurrent;
using System.Security.Claims;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Services;

public sealed class ChatMessageService(CoachPulseDbContext db)
{
    private static readonly ConcurrentDictionary<Guid, (DateTime Start, int Count)> Windows = new();

    public async Task<Message?> SendAsync(Guid senderId, Guid tenantId, string role, Guid clientUserId, string? content, IReadOnlyCollection<Guid>? fileIds, CancellationToken ct, Guid? recipientUserId = null)
    {
        var client = await db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.UserId == clientUserId && c.TenantId == tenantId, ct);
        if (client is null) return null;
        if (role == "Client" && (client.UserId != senderId || client.CoachId is null && !recipientUserId.HasValue)) return null;
        if (role == "CoachStaff" && client.CoachId != senderId) return null;
        if (role is not ("Client" or "CoachStaff" or "TenantOwner")) return null;
        if (role == "Client" && recipientUserId.HasValue && recipientUserId != client.CoachId && !await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id == recipientUserId && x.TenantId == tenantId && x.Role == CoachPulse.Domain.Enums.UserRole.TenantOwner && !x.IsDeleted, ct)) return null;
        var receiverId = role == "Client" ? recipientUserId ?? client.CoachId!.Value : client.UserId;
        var body = content?.Trim() ?? "";
        var attachments = fileIds?.Distinct().ToArray() ?? [];
        if (body.Length == 0 && attachments.Length == 0 || body.Length > 2000 || attachments.Length > 10) throw new ArgumentException("Message must contain text or attachments and be within limits.");
        var now = DateTime.UtcNow;
        var window = Windows.AddOrUpdate(senderId, (now, 1), (_, w) => now - w.Start >= TimeSpan.FromMinutes(1) ? (now, 1) : (w.Start, w.Count + 1));
        if (window.Count > 30) throw new InvalidOperationException("Message rate limit exceeded.");
        var files = attachments.Length == 0 ? new List<StoredFile>() : await db.StoredFiles.IgnoreQueryFilters().Where(f => attachments.Contains(f.Id) && f.TenantId == tenantId && f.OwnerUserId == senderId && f.Purpose == "attachment" && !f.IsDeleted && f.LinkedEntityId == null).ToListAsync(ct);
        if (files.Count != attachments.Length) throw new KeyNotFoundException("Attachment not found.");
        var message = new Message { Id = Guid.NewGuid(), TenantId = tenantId, SenderId = senderId, ReceiverId = receiverId, Content = body, Timestamp = now };
        db.Messages.Add(message);
        foreach (var file in files) { file.LinkedEntityType = "Message"; file.LinkedEntityId = message.Id; }
        await db.SaveChangesAsync(ct);
        return message;
    }

    public static string Group(Guid a, Guid b) { var ids = new[] { a.ToString("N"), b.ToString("N") }; Array.Sort(ids, StringComparer.Ordinal); return $"conversation:{ids[0]}:{ids[1]}"; }
}
