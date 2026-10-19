using System.Security.Claims;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Application.DTOs.Account;
using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Services;
using CoachPulse.Domain.Enums;
using CoachPulse.Domain.Entities;
using CoachPulse.Api.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.WebUtilities;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly CoachPulseDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBackgroundJobClient? _backgroundJobs;
    private readonly IFileStorage? _fileStorage;

    public AccountController(CoachPulseDbContext context, IPasswordHasher passwordHasher, IBackgroundJobClient? backgroundJobs = null, IFileStorage? fileStorage = null)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _backgroundJobs = backgroundJobs;
        _fileStorage = fileStorage;
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
        user.MustChangePassword = false;
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
        if (user.Role == UserRole.TenantOwner && user.TenantId.HasValue)
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

    [HttpPost("data-export")]
    [ProducesResponseType(typeof(DataExportAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestDataExport(CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        if (_backgroundJobs is null) return StatusCode(503);
        var now = DateTime.UtcNow;
        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(ct) : null;
        if (_context.Database.IsNpgsql())
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({userId.ToString()}, 0))", ct);
        if (await _context.DataExportRequests.IgnoreQueryFilters().AnyAsync(x => x.UserId == userId && x.CreatedAt > now.AddHours(-24), ct))
            return StatusCode(429, new ProblemDetails { Status = 429, Title = "A data export was already requested in the last 24 hours." });
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        Guid? tenantId = Guid.TryParse(User.FindFirst("tenantId")?.Value, out var tenant) ? tenant : null;
        var export = new DataExportRequest { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, Status = "Pending", DownloadTokenHash = tokenHash, CreatedAt = now };
        _context.DataExportRequests.Add(export);
        await _context.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        _backgroundJobs.Enqueue<GenerateUserDataExportJob>(job => job.RunAsync(export.Id, CancellationToken.None));
        return AcceptedAtAction(nameof(DataExportStatus), new { id = export.Id }, new DataExportAcceptedResponse(export.Id, export.Status, token, Url.ActionLink(nameof(DataExportStatus), values: new { id = export.Id }), Url.ActionLink(nameof(DownloadDataExport), values: new { id = export.Id })));
    }

    [HttpGet("data-export/{id:guid}")]
    [ProducesResponseType(typeof(DataExportStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DataExportStatus(Guid id, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var export = await _context.DataExportRequests.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        return export is null ? NotFound() : Ok(new DataExportStatusResponse(export.Id, export.Status, export.CreatedAt, export.ExpiresAt, export.DownloadedAt, export.FailureCode));
    }

    [HttpGet("data-export/{id:guid}/download")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDataExport(Guid id, [FromQuery] string token, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        if (_fileStorage is null) return StatusCode(503);
        var export = await _context.DataExportRequests.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (export is null || export.Status != "Ready" || export.ExpiresAt <= DateTime.UtcNow || export.DownloadedAt is not null || export.StoragePath is null) return NotFound();
        var supplied = SHA256.HashData(Encoding.UTF8.GetBytes(token ?? ""));
        byte[] expected; try { expected = Convert.FromHexString(export.DownloadTokenHash); } catch { return NotFound(); }
        if (!CryptographicOperations.FixedTimeEquals(supplied, expected)) return NotFound();
        var stream = await _fileStorage.OpenReadAsync(export.StoragePath, ct); if (stream is null) return NotFound();
        var claimed = await _context.DataExportRequests.IgnoreQueryFilters().Where(x => x.Id == id && x.UserId == userId && x.DownloadedAt == null && x.Status == "Ready" && x.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.DownloadedAt, DateTime.UtcNow), ct);
        if (claimed != 1) { await stream.DisposeAsync(); return NotFound(); }
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, "application/json", "personal-data-export.json");
    }

    private bool TryUserId(out Guid id) => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out id);

    private async Task RevokeAllRefreshTokens(Guid userId)
    {
        var active = await _context.RefreshTokens.IgnoreQueryFilters().Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var token in active) token.RevokedAt = DateTime.UtcNow;
    }
}
