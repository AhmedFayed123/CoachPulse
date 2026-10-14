using CoachPulse.Api.Models;
using CoachPulse.Application.DTOs.Coach;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CoachPulse.Api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class CheckInsController : ControllerBase
    {
        private readonly CoachPulseDbContext _context;
        private readonly IFileStorage _fileStorage;
        private readonly IClientAccessService _clientAccess;
        public CheckInsController(CoachPulseDbContext context, IFileStorage fileStorage, IClientAccessService clientAccess)
        {
            _context = context;
            _fileStorage = fileStorage;
            _clientAccess = clientAccess;
        }

        // GET: /api/clients/{clientId}/checkins
        // Owner: all clients in the tenant.
        // Staff: assigned clients only.
        [HttpGet("clients/{clientId:guid}/checkins")]
        [Authorize(Policy = "CoachOnly")]
        public async Task<IActionResult> GetClientCheckIns(Guid clientId)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            if (!Enum.TryParse<UserRole>(User.FindFirst(ClaimTypes.Role)?.Value, out var role) || role is not (UserRole.Owner or UserRole.Staff))
            {
                return Forbid();
            }

            var client = await _clientAccess.VisibleClients(tenantId, userId, role).FirstOrDefaultAsync(c => c.Id == clientId);

            if (client is null)
            {
                // Do not disclose whether another tenant's client exists.
                return NotFound(new
                {
                    message = "Client not found."
                });
            }

            var checkIns = await _context.CheckIns
                .AsNoTracking()
                .Where(c => c.ClientId == client.Id && c.TenantId == tenantId)
                .OrderByDescending(c => c.Date)
                .Select(c => new
                {
                    c.Id,
                    c.ClientId,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    HasPhoto = c.PhotoUrl != null
                })
                .ToListAsync();

            var result = checkIns.Select(c => new
            {
                c.Id,
                c.ClientId,
                c.Date,
                c.Weight,
                c.Measurements,
                PhotoUrl = c.HasPhoto
                    ? $"/api/my/checkins/{c.Id}/photo"
                    : null
            });

            return Ok(result);
        }

        // POST: /api/my/checkins
        // Client creates a check-in for their own account.

        [HttpPost("my/checkins")]
        [Authorize(Policy = "ClientOnly")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> CreateMyCheckIn(
            [FromForm] CreateCheckInFormRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            var client = await _context.Clients
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.TenantId == tenantId);

            if (client is null)
            {
                return NotFound(new
                {
                    message = "Client profile not found."
                });
            }

            string? storageKey = null;
            string? photoExtension = null;

            if (request.Photo is not null)
            {
                const long maxPhotoSize = 5 * 1024 * 1024;

                if (request.Photo.Length == 0 ||
                    request.Photo.Length > maxPhotoSize)
                {
                    return BadRequest(new
                    {
                        message = "Photo must be between 1 byte and 5 MB."
                    });
                }

                var detectedExtension = await DetectImageExtensionAsync(
                    request.Photo,
                    cancellationToken);

                if (detectedExtension is null)
                {
                    return BadRequest(new
                    {
                        message = "Only valid JPEG, PNG, and WebP images are allowed."
                    });
                }

                var folder = $"{tenantId:N}/{client.Id:N}";
                photoExtension = detectedExtension;

                await using var stream = request.Photo.OpenReadStream();

                storageKey = await _fileStorage.SaveAsync(
                    stream,
                    folder,
                    detectedExtension,
                    cancellationToken);
            }

            var checkIn = new CoachPulse.Domain.Entities.CheckIn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ClientId = client.Id,
                Date = DateTime.UtcNow,
                Weight = request.Weight,
                Measurements = request.Measurements?.Trim(),
                PhotoUrl = storageKey
            };

            StoredFile? storedPhoto = null;
            if (request.Photo is not null && storageKey is not null)
            {
                var detectedContentType = photoExtension switch
                {
                    ".jpg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => throw new InvalidOperationException("Unexpected check-in photo type.")
                };

                await using var hashStream = request.Photo.OpenReadStream();
                storedPhoto = new StoredFile
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OwnerUserId = userId,
                    Purpose = "checkin-photo",
                    OriginalName = GetSafeOriginalName(request.Photo.FileName),
                    ContentType = detectedContentType,
                    SizeBytes = request.Photo.Length,
                    StoragePath = storageKey,
                    Sha256 = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, cancellationToken)).ToLowerInvariant(),
                    CreatedAt = DateTime.UtcNow,
                    LinkedEntityType = "CheckIn",
                    LinkedEntityId = checkIn.Id,
                    IsDeleted = false
                };
            }

            try
            {
                _context.CheckIns.Add(checkIn);
                if (storedPhoto is not null)
                    _context.StoredFiles.Add(storedPhoto);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                if (storageKey is not null)
                    await _fileStorage.DeleteAsync(storageKey, cancellationToken);

                throw;
            }

            return Created(
                $"/api/my/checkins/{checkIn.Id}",
                new
                {
                    message = "Check-in created successfully.",
                    checkIn.Id,
                    checkIn.ClientId,
                    checkIn.Date,
                    checkIn.Weight,
                    checkIn.Measurements,
                    PhotoUrl = storageKey is null
                        ? null
                        : $"/api/my/checkins/{checkIn.Id}/photo"
                });
        }

        private static async Task<string?> DetectImageExtensionAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var header = new byte[12];

            await using var stream = file.OpenReadStream();

            var bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length),
                cancellationToken);

            // JPEG
            if (bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return ".jpg";
            }

            // PNG
            if (bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                return ".png";
            }

            // WebP: RIFF....WEBP
            if (bytesRead >= 12 &&
                header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'E' &&
                header[10] == (byte)'B' &&
                header[11] == (byte)'P')
            {
                return ".webp";
            }

            return null;
        }


        // GET: /api/my/checkins
        // Client sees only their own check-ins.
        [HttpGet("my/checkins")]
        [Authorize(Policy = "ClientOnly")]
        public async Task<IActionResult> GetMyCheckIns()
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.TenantId == tenantId);

            if (client is null)
            {
                return NotFound(new
                {
                    message = "Client profile not found."
                });
            }
            var checkIns = await _context.CheckIns
                .AsNoTracking()
                .Where(c => c.ClientId == client.Id && c.TenantId == tenantId)
                .OrderByDescending(c => c.Date)
                .Select(c => new
                {
                    c.Id,
                    c.ClientId,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    HasPhoto = c.PhotoUrl != null
                })
                .ToListAsync();

            var result = checkIns.Select(c => new
            {
                c.Id,
                c.ClientId,
                c.Date,
                c.Weight,
                c.Measurements,
                PhotoUrl = c.HasPhoto
                    ? $"/api/my/checkins/{c.Id}/photo"
                    : null
            });

            return Ok(result);
        }

        private bool TryGetIdentity(
            out Guid userId,
            out Guid tenantId)
        {
            userId = Guid.Empty;
            tenantId = Guid.Empty;

            return Guid.TryParse(
                        User.FindFirst(
                            ClaimTypes.NameIdentifier)?.Value,
                        out userId)
                    && Guid.TryParse(
                        User.FindFirst("tenantId")?.Value,
                        out tenantId);
        }

        [HttpGet("my/checkins/{checkInId:guid}/photo")]
        [Authorize(Policy = "ClientOnly")]
        [Obsolete("Use GET /api/files/{id}.")]
        public async Task<IActionResult> GetMyCheckInPhoto(
            Guid checkInId,
            CancellationToken cancellationToken)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.TenantId == tenantId);

            if (client is null)
                return NotFound();

            var checkIn = await _context.CheckIns
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == checkInId &&
                    c.ClientId == client.Id &&
                    c.TenantId == tenantId);

            if (checkIn is null || string.IsNullOrWhiteSpace(checkIn.PhotoUrl))
                return NotFound(new { message = "Photo not found." });

            var storedPhoto = await _context.StoredFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(file =>
                    file.LinkedEntityType == "CheckIn" &&
                    file.LinkedEntityId == checkIn.Id &&
                    file.Purpose == "checkin-photo" &&
                    !file.IsDeleted,
                    cancellationToken);

            var storagePath = storedPhoto?.StoragePath ?? checkIn.PhotoUrl;
            var stream = await _fileStorage.OpenReadAsync(
                storagePath,
                cancellationToken);

            if (stream is null)
                return NotFound(new { message = "Photo file not found." });

            var contentType = storedPhoto?.ContentType ?? Path.GetExtension(checkIn.PhotoUrl)
                .ToLowerInvariant() switch
            {
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return File(stream, contentType);
        }

        private static string GetSafeOriginalName(string? suppliedName)
        {
            var normalized = (suppliedName ?? "check-in-photo").Replace('\\', '/');
            var leafName = normalized[(normalized.LastIndexOf('/') + 1)..];
            return string.IsNullOrWhiteSpace(leafName) ? "check-in-photo" : leafName[..Math.Min(leafName.Length, 255)];
        }
        [HttpGet("my/progress")]
        [Authorize(Policy = "ClientOnly")]
        public async Task<IActionResult> GetMyProgress(
    CancellationToken cancellationToken)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId &&
                         c.TenantId == tenantId,
                    cancellationToken);

            if (client is null)
            {
                return NotFound(new
                {
                    message = "Client profile not found."
                });
            }

            var checkIns = await _context.CheckIns
                .AsNoTracking()
                .Where(c =>
                    c.ClientId == client.Id &&
                    c.TenantId == tenantId)
                .OrderBy(c => c.Date)
                .Select(c => new
                {
                    c.Id,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    HasPhoto = c.PhotoUrl != null
                })
                .ToListAsync(cancellationToken);

            if (checkIns.Count == 0)
            {
                return Ok(new
                {
                    ClientId = client.Id,
                    TotalCheckIns = 0,
                    FirstWeight = (decimal?)null,
                    CurrentWeight = (decimal?)null,
                    WeightChange = (decimal?)null,
                    LatestMeasurements = (string?)null,
                    CheckIns = checkIns.Select(c => new
                    {
                        c.Id,
                        c.Date,
                        c.Weight,
                        c.Measurements,
                        PhotoUrl = (string?)null
                    })
                });
            }

            var firstCheckIn = checkIns[0];
            var latestCheckIn = checkIns[^1];

            var result = new
            {
                ClientId = client.Id,
                TotalCheckIns = checkIns.Count,

                FirstWeight = firstCheckIn.Weight,
                CurrentWeight = latestCheckIn.Weight,

                // Positive = weight increased, negative = weight decreased.
                WeightChange =
                    latestCheckIn.Weight - firstCheckIn.Weight,

                LatestMeasurements = latestCheckIn.Measurements,

                CheckIns = checkIns.Select(c => new
                {
                    c.Id,
                    c.Date,
                    c.Weight,
                    c.Measurements,

                    PhotoUrl = c.HasPhoto
                        ? $"/api/my/checkins/{c.Id}/photo"
                        : null
                })
            };

            return Ok(result);
        }
        [HttpGet("clients/{clientId:guid}/progress")]
        [Authorize(Policy = "CoachOnly")]
        public async Task<IActionResult> GetClientProgress(
    Guid clientId,
    CancellationToken cancellationToken)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            if (!Enum.TryParse<UserRole>(User.FindFirst(ClaimTypes.Role)?.Value, out var role) || role is not (UserRole.Owner or UserRole.Staff))
                return Forbid();

            var client = await _clientAccess.VisibleClients(tenantId, userId, role)
                .AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

            if (client is null)
            {
                return NotFound(new
                {
                    message = "Client not found."
                });
            }

            var checkIns = await _context.CheckIns
                .AsNoTracking()
                .Where(c =>
                    c.ClientId == client.Id &&
                    c.TenantId == tenantId)
                .OrderBy(c => c.Date)
                .Select(c => new
                {
                    c.Id,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    HasPhoto = c.PhotoUrl != null
                })
                .ToListAsync(cancellationToken);

            var firstCheckIn = checkIns.FirstOrDefault();
            var latestCheckIn = checkIns.LastOrDefault();

            return Ok(new
            {
                ClientId = client.Id,
                TotalCheckIns = checkIns.Count,

                FirstWeight = firstCheckIn?.Weight,
                CurrentWeight = latestCheckIn?.Weight,

                WeightChange = firstCheckIn is not null &&
                               latestCheckIn is not null
                    ? latestCheckIn.Weight - firstCheckIn.Weight
                    : (decimal?)null,

                LatestMeasurements = latestCheckIn?.Measurements,

                CheckIns = checkIns.Select(c => new
                {
                    c.Id,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    c.HasPhoto
                })
            });
        }
    }
}
