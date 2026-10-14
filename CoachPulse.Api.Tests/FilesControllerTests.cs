using System.Security.Claims;
using CoachPulse.Api.Controllers;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class FilesControllerTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public async Task Upload_ValidPng_CreatesFileRecordAndReturns201()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = new UploadFileRequest
        {
            File = Form(PngSignature, "avatar.png", "text/plain"),
            Purpose = "avatar"
        };
        var result = await fixture.Controller.Upload(request, default);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var stored = await fixture.Db.StoredFiles.SingleAsync();
        Assert.Equal("image/png", stored.ContentType);
        Assert.Equal("avatar", stored.Purpose);
        Assert.Equal(PngSignature.Length, stored.SizeBytes);
        Assert.Equal(64, stored.Sha256.Length);
        Assert.Equal("avatar.png", stored.OriginalName);
    }

    [Fact]
    public async Task Upload_DisallowedContentType_Returns400()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = new UploadFileRequest
        {
            File = Form("plain text"u8.ToArray(), "note.txt", "text/plain"),
            Purpose = "avatar"
        };
        var result = await fixture.Controller.Upload(request, default);
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(fixture.Db.StoredFiles);
    }

    [Fact]
    public async Task Upload_ExceedsPurposeLimit_Returns413()
    {
        await using var fixture = await Fixture.CreateAsync();
        var content = new byte[5 * 1024 * 1024 + 1];
        var request = new UploadFileRequest
        {
            File = Form(content, "large.png", "image/png"),
            Purpose = "avatar"
        };
        var result = await fixture.Controller.Upload(request, default);
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, objectResult.StatusCode);
    }

    [Fact]
    public async Task Upload_PngExtensionWithExecutableBytes_Returns400()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = new UploadFileRequest
        {
            File = Form([0x4D, 0x5A, 0x90, 0x00], "payload.png", "image/png"),
            Purpose = "avatar"
        };
        var result = await fixture.Controller.Upload(request, default);
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(fixture.Db.StoredFiles);
    }

    [Fact]
    public async Task Get_OtherClientsCheckInPhoto_Returns404()
    {
        await using var fixture = await Fixture.CreateAsync();
        var file = fixture.File(purpose: "checkin-photo", owner: Guid.NewGuid());
        fixture.Db.StoredFiles.Add(file);
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.Get(file.Id, default);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_FileFromAnotherTenant_Returns404()
    {
        await using var fixture = await Fixture.CreateAsync();
        var file = fixture.File(purpose: "attachment", owner: fixture.UserId);
        fixture.Db.StoredFiles.Add(file);
        await fixture.Db.SaveChangesAsync();
        fixture.Tenant.SetTenant(Guid.NewGuid());

        var result = await fixture.Controller.Get(file.Id, default);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReceiptLinkedToPayment_Returns409()
    {
        await using var fixture = await Fixture.CreateAsync();
        var file = fixture.File(purpose: "receipt", owner: fixture.UserId);
        file.LinkedEntityType = "Payment";
        file.LinkedEntityId = Guid.NewGuid();
        fixture.Db.StoredFiles.Add(file);
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.Delete(file.Id, default);
        Assert.IsType<ConflictObjectResult>(result);
        Assert.False(file.IsDeleted);
    }

    private static IFormFile Form(byte[] bytes, string fileName, string contentType) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(CoachPulseDbContext db, TenantService tenant, MemoryFileStorage storage, FilesController controller, Guid userId, Guid tenantId)
        {
            Db = db; Tenant = tenant; Storage = storage; Controller = controller; UserId = userId; TenantId = tenantId;
        }

        public CoachPulseDbContext Db { get; }
        public TenantService Tenant { get; }
        public MemoryFileStorage Storage { get; }
        public FilesController Controller { get; }
        public Guid UserId { get; }
        public Guid TenantId { get; }

        public StoredFile File(string purpose, Guid owner) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OwnerUserId = owner,
            Purpose = purpose,
            OriginalName = "file.png",
            ContentType = "image/png",
            SizeBytes = PngSignature.Length,
            StoragePath = "tenant/file.png",
            Sha256 = new string('a', 64),
            CreatedAt = DateTime.UtcNow
        };

        public static async Task<Fixture> CreateAsync()
        {
            var tenant = new TenantService();
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            tenant.SetTenant(tenantId);
            var options = new DbContextOptionsBuilder<CoachPulseDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new CoachPulseDbContext(options, tenant);
            await db.Database.EnsureCreatedAsync();
            var storage = new MemoryFileStorage();
            var controller = new FilesController(db, storage)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim("tenantId", tenantId.ToString()),
                            new Claim(ClaimTypes.Role, "Client")
                        ], "test"))
                    }
                }
            };
            return new Fixture(db, tenant, storage, controller, userId, tenantId);
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TenantService : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; }
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }

    private sealed class MemoryFileStorage : IFileStorage
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken = default)
        {
            var key = $"{folder}/{Guid.NewGuid():N}{extension}";
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            _files[key] = buffer.ToArray();
            return key;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_files.TryGetValue(storageKey, out var bytes) ? (Stream?)new MemoryStream(bytes) : null);

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            _files.Remove(storageKey);
            return Task.CompletedTask;
        }
    }
}