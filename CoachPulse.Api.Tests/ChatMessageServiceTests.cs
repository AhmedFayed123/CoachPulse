using CoachPulse.Api.Services;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class ChatMessageServiceTests
{
    private sealed class CurrentTenant(Guid id) : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; } = id;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }

    [Fact]
    public async Task Send_UsesOnlyTheCurrentAssignedCoachAndPersistsBeforeReturning()
    {
        var tenant = Guid.NewGuid(); var clientId = Guid.NewGuid(); var coachId = Guid.NewGuid(); var otherCoach = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new CoachPulseDbContext(options, new CurrentTenant(tenant));
        db.Users.AddRange(new User { Id = clientId, TenantId = tenant, Email = "client@local", Name = "Client" }, new User { Id = coachId, TenantId = tenant, Email = "coach@local", Name = "Coach" }, new User { Id = otherCoach, TenantId = tenant, Email = "other@local", Name = "Other" });
        db.Clients.Add(new Client { Id = Guid.NewGuid(), TenantId = tenant, UserId = clientId, CoachId = coachId });
        await db.SaveChangesAsync();
        var service = new ChatMessageService(db);

        Assert.Null(await service.SendAsync(clientId, tenant, "Client", otherCoach, "hello", null, default));
        var message = await service.SendAsync(clientId, tenant, "Client", clientId, " hello ", null, default);

        Assert.NotNull(message);
        Assert.Equal(coachId, message.ReceiverId);
        Assert.Equal("hello", message.Content);
        Assert.Equal(message.Id, await db.Messages.Select(m => m.Id).SingleAsync());
    }

    [Fact]
    public async Task Send_RejectsEmptyMessagesWithoutAttachments()
    {
        var tenant = Guid.NewGuid(); var clientId = Guid.NewGuid(); var coachId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new CoachPulseDbContext(options, new CurrentTenant(tenant));
        db.Users.AddRange(new User { Id = clientId, TenantId = tenant, Email = "client@local" }, new User { Id = coachId, TenantId = tenant, Email = "coach@local" });
        db.Clients.Add(new Client { Id = Guid.NewGuid(), TenantId = tenant, UserId = clientId, CoachId = coachId });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new ChatMessageService(db).SendAsync(clientId, tenant, "Client", clientId, "  ", null, default));
        Assert.Empty(await db.Messages.ToListAsync());
    }
}
