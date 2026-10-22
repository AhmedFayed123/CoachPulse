using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoachPulse.Api.Controllers;
using CoachPulse.Application.DTOs.Clients;
using CoachPulse.Application.DTOs.Tenant;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class ClientsTenantTests
{
    [Fact]
    public async Task StaffCannotReadAnotherCoachsClient_Returns404()
    {
        await using var f = await Fixture.Create();
        var client = f.AddClient(coachId: Guid.NewGuid()); await f.Db.SaveChangesAsync();
        f.SetIdentity(Guid.NewGuid(), f.TenantId, UserRole.CoachStaff);
        Assert.IsType<NotFoundResult>(await f.Clients.Get(client.Id));
    }

    [Fact]
    public async Task StaffCannotAssignCoach()
    {
        await using var f = await Fixture.Create(); var client = f.AddClient(f.UserId); await f.Db.SaveChangesAsync();
        f.SetIdentity(f.UserId, f.TenantId, UserRole.CoachStaff);
        Assert.IsType<ForbidResult>(await f.Clients.AssignCoach(client.Id, new AssignCoachRequest { CoachId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task HealthInfoRead_IsAudited()
    {
        await using var f = await Fixture.Create(); var client = f.AddClient(f.UserId); client.HealthInfo = "private"; await f.Db.SaveChangesAsync();
        f.SetIdentity(f.UserId, f.TenantId, UserRole.CoachStaff);
        var response = Assert.IsType<OkObjectResult>(await f.Clients.HealthInfo(client.Id));
        Assert.Contains(await f.Db.SecurityAuditLogs.ToListAsync(), log => log.EventType == "client.health-info.read" && log.Identifier == $"client={client.Id}" && log.UserId == f.UserId);
    }

    [Fact]
    public async Task ClientCannotReadNotes()
    {
        await using var f = await Fixture.Create(); var client = f.AddClient(f.UserId);
        f.Db.ClientNotes.Add(new ClientNote { Id = Guid.NewGuid(), TenantId = f.TenantId, ClientId = client.Id, AuthorUserId = Guid.NewGuid(), Content = "private" }); await f.Db.SaveChangesAsync();
        f.SetIdentity(f.UserId, f.TenantId, UserRole.Client);
        Assert.IsType<NotFoundResult>(await f.Clients.Notes(client.Id));
    }

    [Fact]
    public async Task ResendInvitation_ReplacesStoredTokenHash()
    {
        await using var f = await Fixture.Create();
        var invite = new ClientInvitation { Id = Guid.NewGuid(), TenantId = f.TenantId, CoachId = f.UserId, Email = "client@example.test", TokenHash = Hash("old"), ExpiresAt = DateTime.UtcNow.AddHours(1) };
        f.Db.ClientInvitations.Add(invite); await f.Db.SaveChangesAsync(); f.SetIdentity(f.UserId, f.TenantId, UserRole.CoachStaff);
        var result = Assert.IsType<OkObjectResult>(await f.Clients.ResendInvitation(invite.Id));
        var raw = (string)result.Value!.GetType().GetProperty("InvitationToken")!.GetValue(result.Value)!;
        Assert.NotEqual(Hash("old"), invite.TokenHash); Assert.Equal(Hash(raw), invite.TokenHash);
    }

    [Fact]
    public async Task CurrencyChangeWithSubscription_Returns409()
    {
        await using var f = await Fixture.Create();
        f.Db.Subscriptions.Add(new Subscription { Id = Guid.NewGuid(), TenantId = f.TenantId, ClientId = Guid.NewGuid(), PlanId = Guid.NewGuid(), Status = SubscriptionStatus.Active, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30) });
        await f.Db.SaveChangesAsync(); f.SetIdentity(f.UserId, f.TenantId, UserRole.TenantOwner);
        Assert.IsType<ConflictObjectResult>(await f.Tenant.UpdateSettings(new UpdateTenantSettingsRequest { Currency = "USD", TimeZone = "Africa/Cairo", ReminderDays = 7, DefaultLanguage = "ar" }));
    }

    [Fact]
    public async Task ClientReadFromDifferentTenant_Returns404()
    {
        await using var f = await Fixture.Create(); var client = f.AddClient(f.UserId); await f.Db.SaveChangesAsync();
        f.SetIdentity(f.UserId, Guid.NewGuid(), UserRole.TenantOwner);
        Assert.IsType<NotFoundResult>(await f.Clients.Get(client.Id));
    }

    [Fact]
    public async Task TenantEndpointUsesClaimedTenantScope()
    {
        await using var f = await Fixture.Create(); f.SetIdentity(f.UserId, Guid.NewGuid(), UserRole.TenantOwner);
        Assert.IsType<NotFoundResult>(await f.Tenant.GetTenant());
    }

    private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(CoachPulseDbContext db, TenantState tenantState, ClientsController clients, TenantController tenant, Guid tenantId, Guid userId)
        { Db = db; TenantState = tenantState; Clients = clients; Tenant = tenant; TenantId = tenantId; UserId = userId; }
        public CoachPulseDbContext Db { get; } public TenantState TenantState { get; } public ClientsController Clients { get; } public TenantController Tenant { get; } public Guid TenantId { get; } public Guid UserId { get; }
        public static async Task<Fixture> Create()
        {
            var state = new TenantState(); var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); state.SetTenant(tenantId);
            var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, state); await db.Database.EnsureCreatedAsync();
            db.Tenants.Add(new CoachPulse.Domain.Entities.Tenant { Id = tenantId, Name = "Tenant", Slug = tenantId.ToString("N"), Status = "Active" });
            db.Users.Add(new User { Id = userId, TenantId = tenantId, Email = "coach@example.test", Role = UserRole.TenantOwner, PasswordHash = "pw" }); await db.SaveChangesAsync();
            var clients = new ClientsController(db, new ClientAccessService(db), new TestEnvironment(), new TestEmail(), new ConfigurationBuilder().Build()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
            var tenant = new TenantController(db, new TestPasswordHasher(), new ClientAccessService(db)) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
            var fixture = new Fixture(db, state, clients, tenant, tenantId, userId); fixture.SetIdentity(userId, tenantId, UserRole.TenantOwner); return fixture;
        }
        public Client AddClient(Guid? coachId) { var user = new User { Id = Guid.NewGuid(), TenantId = TenantId, Email = $"{Guid.NewGuid():N}@example.test", Role = UserRole.Client }; Db.Users.Add(user); var client = new Client { Id = Guid.NewGuid(), TenantId = TenantId, UserId = user.Id, CoachId = coachId, HealthInfo = "private" }; Db.Clients.Add(client); return client; }
        public void SetIdentity(Guid userId, Guid tenantId, UserRole role)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim("tenantId", tenantId.ToString()), new Claim(ClaimTypes.Role, role.ToString())], "test"));
            Clients.ControllerContext.HttpContext.User = principal; Tenant.ControllerContext.HttpContext.User = principal;
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class TenantState : ICurrentTenantService { public Guid? TenantId { get; private set; } public void SetTenant(Guid id) => TenantId = id; public void Clear() => TenantId = null; }
    private sealed class TestEnvironment : IWebHostEnvironment { public string EnvironmentName { get; set; } = "Development"; public string ApplicationName { get; set; } = "tests"; public string WebRootPath { get; set; } = ""; public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!; public string ContentRootPath { get; set; } = ""; public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!; }
    private sealed class TestPasswordHasher : IPasswordHasher { public string Hash(string password) => password; public bool Verify(string password, string passwordHash) => password == passwordHash; }
    private sealed class TestEmail : IEmailSender { public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}

