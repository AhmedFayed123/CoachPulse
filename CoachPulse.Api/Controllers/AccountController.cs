using System.Security.Claims;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Application.DTOs.Account;
using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Services;
using CoachPulse.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly CoachPulseDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AccountController(CoachPulseDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return NotFound("User not found.");

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Name,
            user.Phone,
            user.Language,
            user.AvatarFileId,
            user.Role,
            user.TenantId,
            Tenant = user.Tenant == null
                ? null
                : new
                {
                    user.Tenant.Id,
                    user.Tenant.Name,
                    user.Tenant.Slug,
                    user.Tenant.Status
                }
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateAccountRequest request)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return NotFound("User not found.");
        if (request.AvatarFileId.HasValue)
        {
            var avatar = await _context.StoredFiles.FirstOrDefaultAsync(x => x.Id == request.AvatarFileId.Value && x.OwnerUserId == userId && x.Purpose == "avatar" && !x.IsDeleted);
            if (avatar is null) return BadRequest("avatarFileId must refer to your own avatar file.");
        }
        user.Name = request.Name?.Trim(); user.Phone = request.Phone?.Trim();
        user.AvatarFileId = request.AvatarFileId;
        if (!string.IsNullOrWhiteSpace(request.Language)) user.Language = request.Language.Trim();
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return NotFound("User not found.");
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash)) return BadRequest("Current password is incorrect.");
        if (!PasswordPolicy.IsValid(request.NewPassword)) return BadRequest(PasswordPolicy.Message);
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await RevokeAllRefreshTokens(userId);
        _context.SecurityAuditLogs.Add(new CoachPulse.Domain.Entities.SecurityAuditLog { Id = Guid.NewGuid(), UserId = userId, TenantId = user.TenantId, EventType = "password.changed", IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null) return NotFound("User not found.");
        if (user.Role == UserRole.Owner && user.TenantId.HasValue)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == user.TenantId.Value);
            if (tenant is not null && string.Equals(tenant.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                await _context.Clients.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenant.Id))
                return Conflict("Transfer or deactivate the tenant before deleting its owner account.");
        }
        user.IsDeleted = true; user.DeletedAt = DateTime.UtcNow; user.Name = null; user.Phone = null;
        user.Email = $"deleted+{user.Id:N}@invalid.local"; user.AvatarFileId = null;
        user.PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N") + "Aa1!");
        await RevokeAllRefreshTokens(userId);
        _context.SecurityAuditLogs.Add(new CoachPulse.Domain.Entities.SecurityAuditLog { Id = Guid.NewGuid(), UserId = userId, TenantId = user.TenantId, EventType = "account.deleted", IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString() });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out id);

    private async Task RevokeAllRefreshTokens(Guid userId)
    {
        var active = await _context.RefreshTokens.IgnoreQueryFilters().Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var token in active) token.RevokedAt = DateTime.UtcNow;
    }
}
