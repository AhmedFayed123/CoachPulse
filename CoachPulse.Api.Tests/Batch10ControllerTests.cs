using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoachPulse.Api.Controllers;
using CoachPulse.Api.Hubs;
using CoachPulse.Api.Services;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class Batch10ControllerTests
{
    private sealed class CurrentTenant(Guid id) : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; } = id;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }

    private static CoachPulseDbContext CreateDb(Guid tenant) => new(
        new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new CurrentTenant(tenant));

    [Fact]
    public async Task PublicTenantReturnsBrandingOnlyAndHidesSuspendedTenant()
    {
        var tenantId = Guid.NewGuid(); await using var db = CreateDb(Guid.NewGuid());
        db.Tenants.Add(new Tenant { Id = tenantId, Slug = "studio-a", Name = "Studio", Status = "Active", LogoUrl = "/logo.png", PrimaryColor = "#123456", InstaPayId = "private" });
        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Slug = "studio-b", Name = "Paused", Status = "Suspended" }); await db.SaveChangesAsync();
        var controller = new PublicController(db, new MemoryCache(new MemoryCacheOptions()));
        var active = Assert.IsType<OkObjectResult>(await controller.Tenant("studio-a", default));
        var propertyNames = active.Value!.GetType().GetProperties().Select(x => x.Name).ToArray();
        Assert.Contains("Name", propertyNames); Assert.Contains("LogoUrl", propertyNames); Assert.DoesNotContain("TenantId", propertyNames); Assert.DoesNotContain("InstaPayId", propertyNames);
        Assert.IsType<NotFoundResult>(await controller.Tenant("studio-b", default));
    }

    [Fact]
    public async Task NotificationReadCannotAccessAnotherUserNotification()
    {
        var tenant = Guid.NewGuid(); var user = Guid.NewGuid(); var other = Guid.NewGuid(); var id = Guid.NewGuid(); await using var db = CreateDb(tenant);
        db.Notifications.Add(new Notification { Id = id, UserId = other, TenantId = tenant, Type = "Notice", Title = "Private", Body = "body" }); await db.SaveChangesAsync();
        var controller = new NotificationsController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()), new Claim("tenantId", tenant.ToString()) }, "test"));
        Assert.IsType<NotFoundResult>(await controller.Read(id, default));
    }

    [Fact]
    public async Task PublicInvitationReturnsNameAndMaskedEmailOnly()
    {
        var tenant = Guid.NewGuid(); const string token = "sample-invite-token"; await using var db = CreateDb(Guid.NewGuid());
        db.Tenants.Add(new Tenant { Id = tenant, Slug = "studio-a", Name = "Private Studio", Status = "Active" });
        db.ClientInvitations.Add(new ClientInvitation { Id = Guid.NewGuid(), TenantId = tenant, CoachId = Guid.NewGuid(), Email = "alex@example.test", InvitedName = "Alex", TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), ExpiresAt = DateTime.UtcNow.AddHours(2) });
        await db.SaveChangesAsync();
        var controller = new PublicController(db, new MemoryCache(new MemoryCacheOptions()));
        var response = Assert.IsType<OkObjectResult>(await controller.Invitation(token, default));
        var properties = response.Value!.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(response.Value));
        Assert.Equal("Alex", properties["name"]); Assert.Equal("a***@example.test", properties["email"]); Assert.DoesNotContain("tenant", properties.Keys);
    }

    [Fact]
    public async Task NotificationPreferencesCanSuppressNonCriticalDelivery()
    {
        var tenant = Guid.NewGuid(); var userId = Guid.NewGuid(); await using var db = CreateDb(tenant);
        db.Users.Add(new User { Id = userId, TenantId = tenant, Email = "client@example.test", Role = CoachPulse.Domain.Enums.UserRole.Client });
        db.NotificationPreferences.Add(new NotificationPreference { UserId = userId, TenantId = tenant, Type = "CheckInReminder", InAppEnabled = false, EmailEnabled = false });
        await db.SaveChangesAsync(); var email = new EmailSpy();
        var service = new NotificationService(db, new UnusedNotificationHub(), email);
        await service.NotifyAsync(userId, tenant, "CheckInReminder", "Reminder", "Body");
        Assert.Empty(await db.Notifications.IgnoreQueryFilters().ToListAsync()); Assert.Equal(0, email.Sent);
    }

    private sealed class EmailSpy : IEmailSender
    {
        public int Sent { get; private set; }
        public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default) { Sent++; return Task.CompletedTask; }
    }

    private sealed class UnusedNotificationHub : IHubContext<NotificationsHub>
    {
        public IHubClients Clients => throw new InvalidOperationException("Disabled preference must not broadcast.");
        public IGroupManager Groups => throw new NotSupportedException();
    }
}
