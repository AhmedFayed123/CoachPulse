using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoachPulse.Api.Controllers;
using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class AuthAccountSecurityTests
{
    [Fact]
    public async Task Refresh_RotatesHashedToken_AndReuseRevokesAll()
    {
        await using var f = await Fixture.Create();
        var user = f.User(); f.Db.Users.Add(user); await f.Db.SaveChangesAsync();
        var login = Assert.IsType<OkObjectResult>(await f.Auth.Login(new LoginRequest { Email = user.Email, Password = "pw" }));
        var raw = (string)login.Value!.GetType().GetProperty("RefreshToken")!.GetValue(login.Value)!;
        var saved = await f.Db.RefreshTokens.SingleAsync();
        Assert.Equal(Fixture.Hash(raw), saved.TokenHash); Assert.NotEqual(raw, saved.TokenHash);
        var rotated = Assert.IsType<OkObjectResult>(await f.Auth.RefreshToken(new RefreshTokenRequest { RefreshToken = raw }));
        var replacement = (string)rotated.Value!.GetType().GetProperty("RefreshToken")!.GetValue(rotated.Value)!;
        Assert.NotEqual(raw, replacement); Assert.NotNull(saved.RevokedAt);
        Assert.IsType<UnauthorizedObjectResult>(await f.Auth.RefreshToken(new RefreshTokenRequest { RefreshToken = raw }));
        Assert.All(await f.Db.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));
        Assert.Contains(await f.Db.SecurityAuditLogs.ToListAsync(), x => x.EventType == "refresh-token.reuse-detected");
    }

    [Fact]
    public async Task LogoutAll_RevokesEveryRefreshToken()
    {
        await using var f = await Fixture.Create(); var u = f.User(); f.Db.Users.Add(u);
        f.Db.RefreshTokens.AddRange(f.Token(u.Id, "one"), f.Token(u.Id, "two")); await f.Db.SaveChangesAsync();
        f.SetUser(u.Id); Assert.IsType<NoContentResult>(await f.Auth.LogoutAll());
        Assert.All(await f.Db.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsSameGenericMessage()
    {
        await using var f = await Fixture.Create(); var tenant = f.Tenant(); var user = f.User(tenant.Id); f.Db.Tenants.Add(tenant); f.Db.Users.Add(user); await f.Db.SaveChangesAsync();
        var existing = Assert.IsType<OkObjectResult>(await f.Auth.ForgotPassword(new TenantEmailRequest { TenantSlug = tenant.Slug, Email = user.Email }));
        var missing = Assert.IsType<OkObjectResult>(await f.Auth.ForgotPassword(new TenantEmailRequest { TenantSlug = "missing", Email = "nobody@example.test" }));
        var existingMessage = existing.Value!.GetType().GetProperty("Message")!.GetValue(existing.Value);
        var missingMessage = missing.Value!.GetType().GetProperty("Message")!.GetValue(missing.Value);
        Assert.Equal(existingMessage, missingMessage); Assert.Equal("If the account is eligible, instructions will be sent.", missingMessage);
    }

    [Fact]
    public async Task ResetPassword_ExpiredOrUsedToken_IsRejected()
    {
        await using var f = await Fixture.Create(); var tenant = f.Tenant(); var user = f.User(tenant.Id); f.Db.Tenants.Add(tenant); f.Db.Users.Add(user);
        var expired = new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Purpose = "password-reset", TokenHash = Fixture.Hash("expired"), ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        var used = new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Purpose = "password-reset", TokenHash = Fixture.Hash("used"), ExpiresAt = DateTime.UtcNow.AddMinutes(10), UsedAt = DateTime.UtcNow };
        var valid = new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Purpose = "password-reset", TokenHash = Fixture.Hash("valid"), ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
        f.Db.UserSecurityTokens.AddRange(expired, used, valid); await f.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Auth.ResetPassword(new ResetPasswordRequest { TenantSlug = tenant.Slug, Email = user.Email, Token = "expired", NewPassword = "New@Pass123" }));
        Assert.IsType<BadRequestObjectResult>(await f.Auth.ResetPassword(new ResetPasswordRequest { TenantSlug = tenant.Slug, Email = user.Email, Token = "used", NewPassword = "New@Pass123" }));
        Assert.IsType<OkObjectResult>(await f.Auth.ResetPassword(new ResetPasswordRequest { TenantSlug = tenant.Slug, Email = user.Email, Token = "valid", NewPassword = "New@Pass123" }));
        Assert.IsType<BadRequestObjectResult>(await f.Auth.ResetPassword(new ResetPasswordRequest { TenantSlug = tenant.Slug, Email = user.Email, Token = "valid", NewPassword = "New@Pass123" }));
    }

    [Fact]
    public async Task AcceptStaffInvite_ExpiredRejected_AndBodyCannotChooseRoleOrTenant()
    {
        await using var f = await Fixture.Create(); var tenant = f.Tenant(); f.Db.Tenants.Add(tenant);
        f.Db.StaffInvitations.AddRange(
            new StaffInvitation { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "expired@example.test", TokenHash = Fixture.Hash("expired"), ExpiresAt = DateTime.UtcNow.AddMinutes(-1) },
            new StaffInvitation { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = "staff@example.test", TokenHash = Fixture.Hash("valid"), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
        await f.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Auth.AcceptStaffInvite(new AcceptStaffInviteRequest { Token = "expired", Password = "Strong@Pass1" }));
        Assert.IsType<OkObjectResult>(await f.Auth.AcceptStaffInvite(new AcceptStaffInviteRequest { Token = "valid", Password = "Strong@Pass1" }));
        var created = await f.Db.Users.IgnoreQueryFilters().SingleAsync(); Assert.Equal(UserRole.Staff, created.Role); Assert.Equal(tenant.Id, created.TenantId);
    }

    [Fact]
    public async Task DeleteActiveTenantOwnerWithClients_ReturnsConflict()
    {
        await using var f = await Fixture.Create(); var tenant = f.Tenant(); var owner = f.User(tenant.Id, UserRole.Owner); var clientUser = f.User(tenant.Id, UserRole.Client);
        f.Db.Tenants.Add(tenant); f.TenantState.Set(tenant.Id); f.Db.Users.AddRange(owner, clientUser); f.Db.Clients.Add(new Client { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = clientUser.Id }); await f.Db.SaveChangesAsync();
        f.SetUser(owner.Id); var account = new AccountController(f.Db, f.Passwords) { ControllerContext = f.Auth.ControllerContext };
        Assert.IsType<ConflictObjectResult>(await account.DeleteMe());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(CoachPulseDbContext db, AuthController auth, TenantState tenant, Passwords passwords) { Db = db; Auth = auth; TenantState = tenant; Passwords = passwords; }
        public CoachPulseDbContext Db { get; } public AuthController Auth { get; } public TenantState TenantState { get; } public Passwords Passwords { get; }
        public static async Task<Fixture> Create()
        {
            var tenant = new TenantState(); var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant); await db.Database.EnsureCreatedAsync();
            var auth = new AuthController(db, new Passwords(), new Jwt(), new Tokens(), new Email()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
            return new Fixture(db, auth, tenant, new Passwords());
        }
        public User User(Guid? tenantId = null, UserRole role = UserRole.Client) => new() { Id = Guid.NewGuid(), TenantId = tenantId, Email = $"{Guid.NewGuid():N}@example.test", PasswordHash = "pw", Role = role };
        public Tenant Tenant() => new() { Id = Guid.NewGuid(), Name = "tenant", Slug = Guid.NewGuid().ToString("N"), Status = "Active" };
        public RefreshToken Token(Guid uid, string raw) => new() { Id = Guid.NewGuid(), UserId = uid, TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddDays(1) };
        public void SetUser(Guid id) => Auth.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test"));
        public static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class TenantState : ICurrentTenantService { public Guid? TenantId { get; private set; } public void Set(Guid id) => TenantId = id; public void SetTenant(Guid id) => TenantId = id; public void Clear() => TenantId = null; }
    private sealed class Passwords : IPasswordHasher { public string Hash(string p) => p; public bool Verify(string p, string h) => p == h; }
    private sealed class Tokens : IRefreshTokenService { public string GenerateToken() => Guid.NewGuid().ToString("N"); }
    private sealed class Jwt : IJwtService { public string GenerateAccessToken(User user) => "jwt"; }
    private sealed class Email : IEmailSender { public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}

