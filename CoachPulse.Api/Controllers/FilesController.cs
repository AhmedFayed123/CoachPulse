using System.Security.Claims;
using System.Security.Cryptography;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = default!;
    public string? Purpose { get; set; }
}

[ApiController]
[Route("api/files")]
[Authorize]
public sealed class FilesController : ControllerBase
{
    private const long Megabyte = 1024 * 1024;
    private static readonly HashSet<string> ImageTypes = new(StringComparer.Ordinal)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private readonly CoachPulseDbContext _db;
    private readonly IFileStorage _storage;

    public FilesController(CoachPulseDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(101L * Megabyte)]
    [EnableRateLimiting("files-upload")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var ownerUserId, out var tenantId))
            return Unauthorized();

        var file = request.File;
        var purpose = request.Purpose;

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "A non-empty file is required." });

        var normalizedPurpose = purpose?.Trim().ToLowerInvariant();
        var (maximumSize, allowedTypes) = normalizedPurpose switch
        {
            "avatar" or "logo" or "checkin-photo" => (5 * Megabyte, ImageTypes),
            "receipt" => (5 * Megabyte, new HashSet<string>(ImageTypes, StringComparer.Ordinal) { "application/pdf" }),
            "attachment" => (10 * Megabyte, new HashSet<string>(ImageTypes, StringComparer.Ordinal) { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }),
            "exercise-video" => (100 * Megabyte, new HashSet<string>(StringComparer.Ordinal) { "video/mp4", "video/webm" }),
            _ => (0L, null)
        };

        if (allowedTypes is null)
            return BadRequest(new { message = "purpose must be avatar, logo, checkin-photo, receipt, attachment or exercise-video." });

        if (file.Length > maximumSize)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = $"File exceeds the {maximumSize / Megabyte} MB limit for this purpose." });

        DetectedFileContent? detected;
        await using (var inspectionStream = file.OpenReadStream())
            detected = await FileContentInspector.DetectAsync(inspectionStream, cancellationToken);

        if (detected is null || !allowedTypes.Contains(detected.ContentType))
            return BadRequest(new { message = "The file content does not match an allowed file type for this purpose." });

        var originalName = GetSafeOriginalName(file.FileName);
        if (originalName.Length > 255)
            originalName = originalName[^255..];

        string storagePath;
        string sha256;
        await using (var hashStream = file.OpenReadStream())
            sha256 = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, cancellationToken)).ToLowerInvariant();

        await using (var saveStream = file.OpenReadStream())
            storagePath = await _storage.SaveAsync(saveStream, tenantId.ToString("N"), detected.Extension, cancellationToken);

        var storedFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OwnerUserId = ownerUserId,
            Purpose = normalizedPurpose!,
            OriginalName = originalName,
            ContentType = detected.ContentType,
            SizeBytes = file.Length,
            StoragePath = storagePath,
            Sha256 = sha256,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        try
        {
            _db.StoredFiles.Add(storedFile);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(storagePath, cancellationToken);
            throw;
        }

        return Created($"/api/files/{storedFile.Id}", new
        {
            id = storedFile.Id,
            purpose = storedFile.Purpose,
            originalName = storedFile.OriginalName,
            contentType = storedFile.ContentType,
            sizeBytes = storedFile.SizeBytes
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var tenantId))
            return Unauthorized();

        var storedFile = await _db.StoredFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
        if (storedFile is null || storedFile.TenantId != tenantId)
            return NotFound();

        if (!await CanReadAsync(storedFile, userId, tenantId, cancellationToken))
            return NotFound();

        var stream = await _storage.OpenReadAsync(storedFile.StoragePath, cancellationToken);
        if (stream is null)
            return NotFound();

        return File(stream, storedFile.ContentType, storedFile.OriginalName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(out var userId, out var tenantId))
            return Unauthorized();

        var storedFile = await _db.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
        if (storedFile is null || storedFile.TenantId != tenantId)
            return NotFound();

        var isOwner = storedFile.OwnerUserId == userId;
        var isTenantOwner = User.IsInRole(UserRole.Owner.ToString());
        if (!isOwner && !isTenantOwner)
            return NotFound();

        var linkedToPayment = string.Equals(storedFile.LinkedEntityType, "Payment", StringComparison.OrdinalIgnoreCase) ||
            await _db.Payments.IgnoreQueryFilters().AnyAsync(p => p.TenantId == tenantId && p.ReceiptUrl == storedFile.StoragePath, cancellationToken);
        if (linkedToPayment)
            return Conflict(new { message = "A payment receipt linked to a financial record cannot be deleted." });

        await _storage.DeleteAsync(storedFile.StoragePath, cancellationToken);
        if (string.Equals(storedFile.LinkedEntityType, "CheckIn", StringComparison.OrdinalIgnoreCase) && storedFile.LinkedEntityId.HasValue)
        {
            var checkIn = await _db.CheckIns.FirstOrDefaultAsync(c => c.Id == storedFile.LinkedEntityId.Value && c.TenantId == tenantId, cancellationToken);
            if (checkIn is not null)
                checkIn.PhotoUrl = null;
        }
        storedFile.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanReadAsync(StoredFile file, Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        if (file.Purpose is "avatar" or "logo")
            return true;

        if (file.OwnerUserId == userId || User.IsInRole(UserRole.Owner.ToString()))
            return true;

        if (file.Purpose is not ("checkin-photo" or "receipt"))
            return false;

        if (!User.IsInRole(UserRole.Staff.ToString()))
            return false;

        return await _db.Clients.IgnoreQueryFilters().AsNoTracking().AnyAsync(c =>
            c.TenantId == tenantId && c.UserId == file.OwnerUserId && c.CoachId == userId,
            cancellationToken);
    }

    private bool TryGetIdentity(out Guid userId, out Guid tenantId)
    {
        userId = Guid.Empty;
        tenantId = Guid.Empty;
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) &&
            Guid.TryParse(User.FindFirstValue("tenantId"), out tenantId);
    }

    private static string GetSafeOriginalName(string? suppliedName)
    {
        var normalized = (suppliedName ?? "file").Replace('\\', '/');
        var leafName = new string(normalized[(normalized.LastIndexOf('/') + 1)..].Where(character => !char.IsControl(character)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(leafName) ? "file" : leafName;
    }
}