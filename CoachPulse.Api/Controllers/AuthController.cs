using System.Security.Cryptography;
using System.Text;
using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Application.DTOs;
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
    private string FrontendUrl(string path, string email, string token) =>
        $"{(configuration?["Frontend:BaseUrl"] ?? "http://localhost:8088").TrimEnd('/')}{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    private void Audit(string kind, Guid? userId = null, Guid? tenantId = null, string? identifier = null) =>
        db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, EventType = kind,
            Identifier = identifier, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(), CreatedAt = DateTime.UtcNow
        });

    [AllowAnonymous, HttpPost("register-tenant")]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(TenantRegistrationAcceptedResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Register(RegisterTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.TenantName) || string.IsNullOrWhiteSpace(request.TenantSlug)) return BadRequest("Email, tenant name, and tenant slug are required.");
        var normalizedSlug = request.TenantSlug.Trim().ToLowerInvariant();
        if (!PublicController.IsValidSlug(normalizedSlug)) return BadRequest("Tenant slug is invalid or reserved.");
        if (!PasswordPolicy.IsValid(request.Password)) return BadRequest(PasswordPolicy.Message);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.Email == email)) return BadRequest("Email already exists.");
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == normalizedSlug)) return BadRequest("Tenant slug already exists.");
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = request.TenantName.Trim(), Slug = normalizedSlug, Status = "PendingApproval", DefaultLanguage = "en" };
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant.Id, Email = email, Name = request.Name?.Trim(), PasswordHash = passwords.Hash(request.Password), Role = UserRole.TenantOwner };
        db.Tenants.Add(tenant); db.Users.Add(user); await db.SaveChangesAsync();
        await SendSecurityEmailAsync(user, "email-confirmation", EmailTemplateType.EmailConfirmation, "/confirm-email");
        Audit("tenant.registration.requested", user.Id, tenant.Id, tenant.Slug);
        await db.SaveChangesAsync();
        return Accepted(new TenantRegistrationAcceptedResponse(tenant.Id, "PendingApproval"));
    }

    [AllowAnonymous, HttpPost("register-client")]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
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
        if (coachId.HasValue && !await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id == coachId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive && (x.Role == UserRole.TenantOwner || x.Role == UserRole.CoachStaff))) return BadRequest("Invalid coach for this tenant.");
        var user = new User { Id = Guid.NewGuid(), TenantId = tenantId, Email = email, Name = request.Name?.Trim() ?? invite?.InvitedName, PasswordHash = passwords.Hash(request.Password), Role = UserRole.Client };
        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id, CoachId = coachId, Goals = request.Goals?.Trim(), HealthInfo = request.HealthInfo?.Trim() };
        db.Users.Add(user); db.Clients.Add(client); if (invite is not null) invite.AcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await SendSecurityEmailAsync(user, "email-confirmation", EmailTemplateType.EmailConfirmation, "/confirm-email");
        var response = await CreateAuthResponseAsync(user);
        await db.SaveChangesAsync();
        return Ok(response);
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
        var user = new User { Id = Guid.NewGuid(), TenantId = invite.TenantId, Email = invite.Email, PasswordHash = passwords.Hash(request.Password), Role = UserRole.CoachStaff };
        invite.AcceptedAt = DateTime.UtcNow; db.Users.Add(user); await db.SaveChangesAsync();
        var response = await CreateAuthResponseAsync(user);
        await db.SaveChangesAsync();
        return Ok(response);
    }

    [AllowAnonymous, HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
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
        else
        {
            // A missing tenant slug is reserved for explicitly selected platform administrators.
            // Never search tenant accounts across the whole database on an ambiguous login.
            query = query.Where(x => x.TenantId == null && x.Role == UserRole.SuperAdmin);
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
        Audit("login.succeeded", user.Id, user.TenantId);
        var response = await CreateAuthResponseAsync(user);
        await db.SaveChangesAsync();
        return Ok(response);
    }

    [AllowAnonymous, HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
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
        var response = await CreateAuthResponseAsync(token.User);
        await db.SaveChangesAsync();
        return Ok(response);
    }

    private async Task<AuthSessionResponse> CreateAuthResponseAsync(User user)
    {
        var accessTokenMinutes = configuration?.GetValue("Jwt:AccessTokenMinutes", 15) ?? 15;
        var refreshTokenDays = configuration?.GetValue("Jwt:RefreshTokenDays", 7) ?? 7;
        var rawRefreshToken = refreshTokens.GenerateToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = Hash(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        });
        var tenant = user.TenantId is Guid tenantId
            ? await db.Tenants.IgnoreQueryFilters().Where(x => x.Id == tenantId)
                .Select(x => new AuthSessionTenant(x.Id, x.Slug, x.Name)).FirstOrDefaultAsync()
            : null;
        return new AuthSessionResponse(jwt.GenerateAccessToken(user), rawRefreshToken, accessTokenMinutes * 60,
            new AuthSessionUser(user.Id, user.Name, user.Role), tenant);
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
        var email = request.Email.Trim().ToLowerInvariant();
        var users = db.Users.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.Email == email);
        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var slug = request.TenantSlug.Trim().ToLowerInvariant();
            var tenantId = await db.Tenants.Where(x => x.Slug == slug).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            users = tenantId is null ? users.Where(_ => false) : users.Where(x => x.TenantId == tenantId);
        }
        foreach (var user in await users.ToListAsync())
        {
            var raw = refreshTokens.GenerateToken();
            db.UserSecurityTokens.Add(new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = user.TenantId, Purpose = "password-reset", TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
            await emails.SendTemplateAsync(user.Email, EmailTemplateType.PasswordReset, user.Language,
                new Dictionary<string, string> { ["token"] = raw, ["name"] = user.Name ?? string.Empty, ["actionUrl"] = FrontendUrl("/reset-password", user.Email, raw) }, HttpContext.RequestAborted);
            Audit("password-reset.requested", user.Id, user.TenantId);
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
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> ResendConfirmation(TenantEmailRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var users = db.Users.IgnoreQueryFilters().Where(x => !x.IsDeleted && !x.EmailConfirmed && x.Email == email);
        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var slug = request.TenantSlug.Trim().ToLowerInvariant();
            var tenantId = await db.Tenants.Where(x => x.Slug == slug).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            users = tenantId is null ? users.Where(_ => false) : users.Where(x => x.TenantId == tenantId);
        }
        foreach (var user in await users.ToListAsync())
        {
            var raw = refreshTokens.GenerateToken();
            db.UserSecurityTokens.Add(new UserSecurityToken { Id = Guid.NewGuid(), UserId = user.Id, TenantId = user.TenantId, Purpose = "email-confirmation", TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
            await emails.SendTemplateAsync(user.Email, EmailTemplateType.EmailConfirmation, user.Language,
                new Dictionary<string, string> { ["token"] = raw, ["name"] = user.Name ?? string.Empty, ["actionUrl"] = FrontendUrl("/confirm-email", user.Email, raw) }, HttpContext.RequestAborted);
        }
        await db.SaveChangesAsync(); return Ok(new { Message = UniformResponse });
    }

    private async Task<UserSecurityToken?> FindSecurityToken(string? slug, string email, string raw, string purpose)
    {
        IQueryable<UserSecurityToken> tokens = db.UserSecurityTokens.Where(x => x.Purpose == purpose && x.TokenHash == Hash(raw));
        if (!string.IsNullOrWhiteSpace(slug))
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var tenantId = await db.Tenants.Where(x => x.Slug == normalizedSlug).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            if (tenantId is null) return null;
            tokens = tokens.Where(x => x.TenantId == tenantId);
        }
        var token = await tokens.FirstOrDefaultAsync();
        if (token is null || token.UsedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow) return null;
        return await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id == token.UserId && !x.IsDeleted && x.Email == email.Trim().ToLowerInvariant()) ? token : null;
    }

    private async Task SendSecurityEmailAsync(User user, string purpose, EmailTemplateType template, string path)
    {
        var raw = refreshTokens.GenerateToken();
        db.UserSecurityTokens.Add(new UserSecurityToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TenantId = user.TenantId, Purpose = purpose,
            TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await db.SaveChangesAsync();
        await emails.SendTemplateAsync(user.Email, template, user.Language,
            new Dictionary<string, string>
            {
                ["token"] = raw,
                ["name"] = user.Name ?? string.Empty,
                ["actionUrl"] = FrontendUrl(path, user.Email, raw)
            }, HttpContext.RequestAborted);
    }

    private async Task RevokeAllAsync(Guid userId)
    {
        var active = await db.RefreshTokens.IgnoreQueryFilters().Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var token in active) token.RevokedAt = DateTime.UtcNow;
    }

    private bool CurrentUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
