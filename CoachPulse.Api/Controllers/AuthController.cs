using System.Security.Cryptography;
using System.Text;
using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Services;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    CoachPulseDbContext db,
    IPasswordHasher passwords,
    IJwtService jwt,
    IRefreshTokenService refreshTokens,
    IEmailSender emails,
    IConfiguration? configuration = null) : ControllerBase
{
    private const string UniformResponse = "If the account is eligible, instructions will be sent.";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private void Audit(string kind, Guid? userId = null, Guid? tenantId = null, string? identifier = null) =>
        db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, EventType = kind,
            Identifier = identifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(), CreatedAt = DateTime.UtcNow
        });

    [AllowAnonymous, HttpPost("register-tenant")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> Register(RegisterTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.TenantName) || string.IsNullOrWhiteSpace(request.TenantSlug)) return BadRequest("Email, tenant name, and tenant slug are required.");
        var normalizedSlug = request.TenantSlug.Trim().ToLowerInvariant();
        if (!PublicController.IsValidSlug(normalizedSlug)) return BadRequest("Tenant slug is invalid or reserved.");
        if (!PasswordPolicy.IsValid(request.Password)) return BadRequest(PasswordPolicy.Message);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Email == email)) return BadRequest("Email already exists.");
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == normalizedSlug)) return BadRequest("Tenant slug already exists.");
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = request.TenantName.Trim(), Slug = normalizedSlug, Status = "Active" };
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = email, PasswordHash = passwords.Hash(request.Password), Role = UserRole.Owner };
        db.Tenants.Add(tenant); db.Users.Add(user); await db.SaveChangesAsync();
        return Ok(new { Message = "Registration successful.", UserId = user.Id, TenantId = tenant.Id, Email = user.Email, Role = user.Role.ToString() });
    }

    [AllowAnonymous, HttpPost("register-client")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> RegisterClient(RegisterClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email is required.");
        if (!PasswordPolicy.IsValid(request.Password)) return BadRequest(PasswordPolicy.Message);
        var email = request.Email.Trim().ToLowerInvariant();
        ClientInvitation? invite = null;
        if (!string.IsNullOrWhiteSpace(request.InviteToken))
        {
            invite = await db.ClientInvitations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TokenHash == Hash(request.InviteToken));
            if (invite is null || invite.AcceptedAt.HasValue || invite.ExpiresAt <= DateTime.UtcNow) return BadRequest("Invalid or expired invitation.");
            if (!string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase)) return BadRequest("Invitation email does not match.");
        }
        var tenantId = invite?.TenantId ?? request.TenantId;
        if (tenantId == Guid.Empty) return BadRequest("TenantId or a valid inviteToken is required.");
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null || tenant.IsDeleted) return BadRequest("Tenant not found.");
        if (!string.Equals(tenant.Status, "Active", StringComparison.OrdinalIgnoreCase)) return BadRequest("Tenant is not active.");
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.TenantId == tenantId && x.Email == email)) return BadRequest("Email already exists in this tenant.");
        var coachId = invite?.CoachId ?? request.CoachId;
        if (coachId.HasValue && !await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id == coachId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive && (x.Role == UserRole.Owner || x.Role == UserRole.Staff))) return BadRequest("Invalid coach for this tenant.");
        var user = new User { Id = Guid.NewGuid(), TenantId = tenantId, Email = email, PasswordHash = passwords.Hash(request.Password), Role = UserRole.Client };
        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id, CoachId = coachId, Goals = request.Goals?.Trim(), HealthInfo = request.HealthInfo?.Trim() };
        db.Users.Add(user); db.Clients.Add(client); if (invite is not null) invite.AcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { Message = invite is null ? "Client registration successful." : "Invitation accepted successfully.", UserId = user.Id, ClientId = client.Id, TenantId = tenant.Id, Email = user.Email, Role = user.Role.ToString() });
    }

    [AllowAnonymous, Obsolete("Use register-client with inviteToken.")]
    [HttpPost("accept-invitation")]
    public Task<IActionResult> AcceptInvitation(AcceptInvitationRequest request) => RegisterClient(new RegisterClientRequest
    {
        InviteToken = request.Token,
        Email = ResolveClientInvitationEmail(request.Token),
        Password = request.Password
    });

    private string ResolveClientInvitationEmail(string token)
    {
        // The legacy contract did not carry the invite email; the handler resolves it from the invitation below.
        return db.ClientInvitations.IgnoreQueryFilters().AsNoTracking().FirstOrDefault(x => x.TokenHash == Hash(token))?.Email ?? string.Empty;
    }

    [AllowAnonymous]
    [HttpPost("accept-staff-invite")]
    public async Task<IActionResult> AcceptStaffInvite(AcceptStaffInviteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest("Invitation token is required.");
        if (!PasswordPolicy.IsValid(request.Password)) return BadRequest(PasswordPolicy.Message);
        var invite = await db.StaffInvitations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TokenHash == Hash(request.Token));
        if (invite is null || invite.AcceptedAt.HasValue || invite.ExpiresAt <= DateTime.UtcNow) return BadRequest("Invalid or expired invitation.");
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == invite.TenantId && x.Status == "Active");
        if (tenant is null) return BadRequest("Tenant is not active.");
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.TenantId == invite.TenantId && x.Email == invite.Email)) return BadRequest("An account already exists for this email.");
        var user = new User { Id = Guid.NewGuid(), TenantId = invite.TenantId, Email = invite.Email, PasswordHash = passwords.Hash(request.Password), Role = UserRole.Staff };
        invite.AcceptedAt = DateTime.UtcNow; db.Users.Add(user); await db.SaveChangesAsync();
        return Ok(new { Message = "Invitation accepted successfully.", UserId = user.Id, TenantId = invite.TenantId, Email = user.Email, Role = user.Role.ToString() });
    }

    [AllowAnonymous, HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Email and password are required.");
        var email = request.Email.Trim().ToLowerInvariant();
        var query = db.Users.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.IsActive && x.Email == email);
        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Slug == request.TenantSlug.Trim().ToLowerInvariant());
            if (tenant is null) { Audit("login.failed", identifier: email); await db.SaveChangesAsync(); return Unauthorized("Invalid email or password."); }
            query = query.Where(x => x.TenantId == tenant.Id);
        }
        var matches = await query.ToListAsync();
        var validUsers = matches.Where(x => passwords.Verify(request.Password, x.PasswordHash)).Take(2).ToList();
        var user = validUsers.Count == 1 ? validUsers[0] : null;
        if (user is null) { Audit("login.failed", identifier: email); await db.SaveChangesAsync(); return Unauthorized("Invalid email or password."); }
        if (user.TenantId.HasValue)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == user.TenantId.Value);
            if (tenant is null || tenant.IsDeleted || !string.Equals(tenant.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                Audit("login.failed", user.Id, user.TenantId, user.Email); await db.SaveChangesAsync();
                return Unauthorized("Tenant is not active.");
            }
        }
        var raw = refreshTokens.GenerateToken();
        var accessTokenMinutes = configuration?.GetValue("Jwt:AccessTokenMinutes", 15) ?? 15;
        var refreshTokenDays = configuration?.GetValue("Jwt:RefreshTokenDays", 7) ?? 7;
        var token = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays) };
        db.RefreshTokens.Add(token); Audit("login.succeeded", user.Id, user.TenantId); await db.SaveChangesAsync();
        return Ok(new { AccessToken = jwt.GenerateAccessToken(user), RefreshToken = raw, TokenType = "Bearer", ExpiresInMinutes = accessTokenMinutes, RefreshTokenExpiresAt = token.ExpiresAt, User = new { user.Id, user.Email, user.TenantId, user.Role } });
    }

    [AllowAnonymous, HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return BadRequest("Refresh token is required.");
        var hash = Hash(request.RefreshToken);
        var token = await db.RefreshTokens.IgnoreQueryFilters().Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (token is null) return Unauthorized("Invalid refresh token.");
        if (token.RevokedAt.HasValue)
        {
            await RevokeAllAsync(token.UserId);
            Audit("refresh-token.reuse-detected", token.UserId, token.User?.TenantId); await db.SaveChangesAsync();
            return Unauthorized("Refresh token reuse detected.");
        }
        if (token.ExpiresAt <= DateTime.UtcNow || token.User is null || token.User.IsDeleted) return Unauthorized("Refresh token has expired.");
        if (token.User.TenantId.HasValue)
        {
            var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == token.User.TenantId.Value);
            if (tenant is null || tenant.IsDeleted || !string.Equals(tenant.Status, "Active", StringComparison.OrdinalIgnoreCase)) return Unauthorized("Tenant is not active.");
        }
        token.RevokedAt = DateTime.UtcNow;
        var raw = refreshTokens.GenerateToken(); var replacement = new RefreshToken { Id = Guid.NewGuid(), UserId = token.UserId, TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddDays(7) };
        db.RefreshTokens.Add(replacement); await db.SaveChangesAsync();
        return Ok(new { AccessToken = jwt.GenerateAccessToken(token.User), RefreshToken = raw, TokenType = "Bearer", ExpiresInMinutes = 60, RefreshTokenExpiresAt = replacement.ExpiresAt });
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        if (!CurrentUser(out var userId)) return Unauthorized();
        var hash = Hash(request.RefreshToken ?? string.Empty);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == hash);
        if (token is not null && token.RevokedAt is null) { token.RevokedAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return NoContent();
    }

    [Authorize, HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        if (!CurrentUser(out var userId)) return Unauthorized();
        await RevokeAllAsync(userId);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [AllowAnonymous, HttpPost("forgot-password")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> ForgotPassword(TenantEmailRequest request)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Slug == request.TenantSlug.Trim().ToLowerInvariant());
        if (tenant is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => !x.IsDeleted && x.TenantId == tenant.Id && x.Email == email);
            if (user is not null)
            {
                var raw = refreshTokens.GenerateToken();
                db.UserSecurityTokens.Add(new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Purpose = "password-reset", TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
                await emails.SendAsync(user.Email, "Password reset", raw, HttpContext.RequestAborted);
                Audit("password-reset.requested", user.Id, tenant.Id);
            }
        }
        await db.SaveChangesAsync(); return Ok(new { Message = UniformResponse });
    }

    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword)) return BadRequest(PasswordPolicy.Message);
        var token = await FindSecurityToken(request.TenantSlug, request.Email, request.Token, "password-reset");
        if (token is null) return BadRequest("Invalid or expired code.");
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == token.UserId && !x.IsDeleted);
        if (user is null) return BadRequest("Invalid or expired code.");
        token.UsedAt = DateTime.UtcNow; user.PasswordHash = passwords.Hash(request.NewPassword);
        await RevokeAllAsync(user.Id);
        Audit("password-reset.completed", user.Id, user.TenantId); await db.SaveChangesAsync(); return Ok(new { Message = "Password reset successful." });
    }

    [AllowAnonymous, HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(EmailTokenRequest request)
    {
        var token = await FindSecurityToken(request.TenantSlug, request.Email, request.Token, "email-confirmation");
        if (token is null) return BadRequest("Invalid or expired code.");
        token.UsedAt = DateTime.UtcNow;
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(x => x.Id == token.UserId && !x.IsDeleted); user.EmailConfirmed = true;
        await db.SaveChangesAsync(); return Ok(new { Message = "Email confirmed." });
    }

    [AllowAnonymous, HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(TenantEmailRequest request)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Slug == request.TenantSlug.Trim().ToLowerInvariant());
        if (tenant is not null)
        {
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => !x.IsDeleted && !x.EmailConfirmed && x.TenantId == tenant.Id && x.Email == request.Email.Trim().ToLowerInvariant());
            if (user is not null)
            {
                var raw = refreshTokens.GenerateToken(); db.UserSecurityTokens.Add(new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenant.Id, Purpose = "email-confirmation", TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
                await emails.SendAsync(user.Email, "Confirm email", raw, HttpContext.RequestAborted);
            }
        }
        await db.SaveChangesAsync(); return Ok(new { Message = UniformResponse });
    }

    private async Task<UserSecurityToken?> FindSecurityToken(string slug, string email, string raw, string purpose)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Slug == slug.Trim().ToLowerInvariant());
        if (tenant is null) return null;
        var token = await db.UserSecurityTokens.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Purpose == purpose && x.TokenHash == Hash(raw));
        if (token is null || token.UsedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow) return null;
        return await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id == token.UserId && !x.IsDeleted && x.Email == email.Trim().ToLowerInvariant()) ? token : null;
    }

    private async Task RevokeAllAsync(Guid userId)
    {
        var active = await db.RefreshTokens.IgnoreQueryFilters().Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var token in active) token.RevokedAt = DateTime.UtcNow;
    }

    private bool CurrentUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
