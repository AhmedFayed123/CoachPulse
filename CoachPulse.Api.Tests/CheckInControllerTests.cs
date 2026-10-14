using System.Security.Claims;
using CoachPulse.Api.Controllers;
using CoachPulse.Api.Models;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class CheckInControllerTests
{
    private sealed class CurrentTenant(Guid id) : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; } = id;
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }
    private sealed class NullStorage : IFileStorage
    {
        public Task<string> SaveAsync(Stream content,string folder,string extension,CancellationToken cancellationToken=default)=>Task.FromResult("unused");
        public Task<Stream?> OpenReadAsync(string storageKey,CancellationToken cancellationToken=default)=>Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string storageKey,CancellationToken cancellationToken=default)=>Task.CompletedTask;
    }
    private static (CoachPulseDbContext Db, CheckInsController Controller) Create(Guid tenant,Guid user,string role)
    {
        var options=new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db=new CoachPulseDbContext(options,new CurrentTenant(tenant));
        var controller=new CheckInsController(db,new NullStorage(),new ClientAccessService(db));
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.ToString()),new Claim("tenantId",tenant.ToString()),new Claim(ClaimTypes.Role,role)};
        controller.ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,"test"))}};
        return(db,controller);
    }

    [Fact]
    public async Task Update_ReturnsConflictAfter24Hours()
    {
        var tenant=Guid.NewGuid();var user=Guid.NewGuid();var client=new Client{Id=Guid.NewGuid(),TenantId=tenant,UserId=user};var (db,api)=Create(tenant,user,"Client");
        db.Clients.Add(client);db.CheckIns.Add(new CheckIn{Id=Guid.NewGuid(),TenantId=tenant,ClientId=client.Id,Client=client,Date=DateTime.UtcNow.AddHours(-25),Weight=80});await db.SaveChangesAsync();
        var checkin=await db.CheckIns.IgnoreQueryFilters().Select(c=>c.Id).SingleAsync();
        var result=await api.UpdateMyCheckIn(checkin,new CreateCheckInUpdateRequest(79,"waist=90"),default);
        Assert.IsType<ConflictObjectResult>(result);await db.DisposeAsync();
    }

    [Fact]
    public async Task ClientCannotLinkAnotherUsersPhoto()
    {
        var tenant=Guid.NewGuid();var user=Guid.NewGuid();var other=Guid.NewGuid();var client=new Client{Id=Guid.NewGuid(),TenantId=tenant,UserId=user};var (db,api)=Create(tenant,user,"Client");
        var crossTenant=Guid.NewGuid();var wrongOwner=Guid.NewGuid();db.Clients.Add(client);db.StoredFiles.AddRange(new StoredFile{Id=Guid.NewGuid(),TenantId=tenant,OwnerUserId=other,Purpose="checkin-photo",OriginalName="photo.png",ContentType="image/png",SizeBytes=8,StoragePath="same-tenant-file",Sha256=new string('a',64),CreatedAt=DateTime.UtcNow},new StoredFile{Id=Guid.NewGuid(),TenantId=crossTenant,OwnerUserId=wrongOwner,Purpose="checkin-photo",OriginalName="photo.png",ContentType="image/png",SizeBytes=8,StoragePath="cross-tenant-file",Sha256=new string('b',64),CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync();
        var fileIds=await db.StoredFiles.IgnoreQueryFilters().Select(f=>f.Id).ToArrayAsync();
        foreach(var fileId in fileIds){var result=await api.CreateMyCheckIn(new CreateCheckInFormRequest{Weight=80,PhotoFileIds=[fileId]},default);Assert.IsType<NotFoundObjectResult>(result);}
        Assert.Empty(await db.CheckIns.IgnoreQueryFilters().ToListAsync());await db.DisposeAsync();
    }

    [Fact]
    public async Task StaffCannotReviewUnassignedClientCheckIn()
    {
        var tenant=Guid.NewGuid();var staff=Guid.NewGuid();var assignedCoach=Guid.NewGuid();var client=new Client{Id=Guid.NewGuid(),TenantId=tenant,UserId=Guid.NewGuid(),CoachId=assignedCoach};var (db,api)=Create(tenant,staff,"Staff");
        db.Clients.Add(client);db.CheckIns.Add(new CheckIn{Id=Guid.NewGuid(),TenantId=tenant,ClientId=client.Id,Client=client,Date=DateTime.UtcNow,Weight=80});await db.SaveChangesAsync();
        var checkin=await db.CheckIns.IgnoreQueryFilters().Select(c=>c.Id).SingleAsync();
        var result=await api.Feedback(client.Id,checkin,new CheckInFeedbackRequest("Good progress"),null!,default);
        Assert.IsType<NotFoundResult>(result);await db.DisposeAsync();
    }
}
