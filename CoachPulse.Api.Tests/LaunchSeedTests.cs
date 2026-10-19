using System.Security.Claims;
using CoachPulse.Api.Middleware;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Data.Seed;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class LaunchSeedTests
{
    [Fact]
    public void DatabaseStartupStateIsNotReadyUntilInitializationSucceeds()
    {
        var state = new DatabaseStartupState();
        Assert.False(state.IsInitialized);

        state.MarkFailure("database unavailable");
        Assert.False(state.IsInitialized);
        Assert.Equal("database unavailable", state.Failure);

        state.MarkInitialized();
        Assert.True(state.IsInitialized);
        Assert.Null(state.Failure);
    }

    [Fact]
    public async Task SuperAdminBootstrapIsIdempotentAndRequiresPasswordChange()
    {
        var tenant = new TenantState();
        await using var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        await db.Database.EnsureCreatedAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SuperAdmin:Email"] = "root@example.test",
            ["SuperAdmin:Password"] = "BootStrap-Only-Password123!"
        }).Build();
        var hasher = new Passwords();

        await DbSeeder.SeedAsync(db, hasher, config, isProduction: true);
        await DbSeeder.SeedAsync(db, hasher, config, isProduction: true);

        var user = await db.Users.IgnoreQueryFilters().SingleAsync(x => x.Role == UserRole.SuperAdmin);
        Assert.True(user.MustChangePassword);
        Assert.Equal("BootStrap-Only-Password123!", user.PasswordHash);
    }

    [Fact]
    public async Task ProductionBootstrapFailsClearlyWhenCredentialsAreMissing()
    {
        var tenant = new TenantState();
        await using var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        await db.Database.EnsureCreatedAsync();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => DbSeeder.SeedAsync(db, new Passwords(), new ConfigurationBuilder().Build(), isProduction: true));
        Assert.Contains("SuperAdmin__Email", ex.Message);
        Assert.DoesNotContain("Password123", ex.Message);
    }

    [Fact]
    public async Task PublicExerciseSeedCanRunTwiceWithoutDuplicates()
    {
        var tenant = new TenantState();
        await using var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        await db.Database.EnsureCreatedAsync();
        var path = Path.Combine(Path.GetTempPath(), $"coachpulse-seed-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(path, "name,muscleGroup,equipment,videoUrl\nGoblet Squat,Legs,Dumbbells,\n");
        try
        {
            Assert.Equal(1, await PublicExerciseSeeder.SeedCsvAsync(db, path));
            Assert.Equal(0, await PublicExerciseSeeder.SeedCsvAsync(db, path));
            Assert.Equal(1, await db.Exercises.IgnoreQueryFilters().CountAsync(x => x.TenantId == null));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task InitialPasswordClaimBlocksOtherRoutesUntilPasswordChange()
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("mustChangePassword", "true")], "test"));
        http.Request.Path = "/api/clients";
        var invoked = false;
        var middleware = new FirstLoginPasswordChangeMiddleware(_ => { invoked = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(http);
        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);

        http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("mustChangePassword", "true")], "test"));
        http.Request.Path = "/api/account/change-password";
        invoked = false;
        await middleware.InvokeAsync(http);
        Assert.True(invoked);
    }

    private sealed class TenantState : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; }
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }

    private sealed class Passwords : IPasswordHasher
    {
        public string Hash(string password) => password;
        public bool Verify(string password, string passwordHash) => password == passwordHash;
    }
}
