using CoachPulse.Application.Interfaces;
using CoachPulse.Api.Controllers;
using CoachPulse.Api.Hubs;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class SubscriptionPaymentCoreTests
{
    [Fact]
    public void OnlyActivationServiceContainsThePendingToActiveMutation()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName,"CoachPulse.Infrastructure","Services","SubscriptionActivationService.cs"))) root=root.Parent;
        Assert.NotNull(root);
        var service=Path.Combine(root!.FullName,"CoachPulse.Infrastructure","Services","SubscriptionActivationService.cs");
        var assignment=new Regex(@"\.Status\s*=\s*SubscriptionStatus\.Active");
        foreach(var file in Directory.EnumerateFiles(Path.Combine(root.FullName,"CoachPulse.Api"),"*.cs",SearchOption.AllDirectories))
            Assert.False(assignment.IsMatch(File.ReadAllText(file)),$"Activation bypass found in {file}");
        foreach(var file in Directory.EnumerateFiles(Path.Combine(root.FullName,"CoachPulse.Infrastructure"),"*.cs",SearchOption.AllDirectories).Where(f=>!string.Equals(f,service,StringComparison.OrdinalIgnoreCase)))
            Assert.False(assignment.IsMatch(File.ReadAllText(file)),$"Activation bypass found in {file}");
    }

    [Fact]
    public async Task ActivationService_ActivatesOnlyPendingSubscriptionAndUsesSnapshotDuration()
    {
        var subscription = new Subscription { Status=SubscriptionStatus.Pending, SnapshotDurationDays=28, StartDate=DateTime.UtcNow.AddDays(-10), EndDate=DateTime.UtcNow };
        var service = new SubscriptionActivationService();
        await service.ActivateAsync(subscription, SubscriptionActivationSource.OwnerManual);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.InRange((subscription.EndDate-subscription.StartDate).TotalDays,27.99,28.01);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ActivateAsync(subscription, SubscriptionActivationSource.PaymobWebhook));
    }

    [Fact]
    public void StateMachine_RejectsIllegalTransitionsAndPreservesPausedDays()
    {
        var states = new SubscriptionActivationService();
        var pending = new Subscription { Status=SubscriptionStatus.Pending };
        Assert.Throws<InvalidOperationException>(()=>states.Transition(pending,SubscriptionStatus.Active));
        var active = new Subscription { Status=SubscriptionStatus.Active,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow.AddDays(12) };
        states.Transition(active,SubscriptionStatus.Paused,"medical leave");
        Assert.Equal(SubscriptionStatus.Paused,active.Status);
        Assert.InRange(active.RemainingDays!.Value,11,13);
        states.Transition(active,SubscriptionStatus.Active,"resumed");
        Assert.Equal(SubscriptionStatus.Active,active.Status);
        Assert.Null(active.RemainingDays);
        Assert.InRange((active.EndDate-DateTime.UtcNow).TotalDays,11,13);
    }

    [Fact]
    public async Task PlanEditDoesNotChangeSubscriptionPriceAndDurationSnapshot()
    {
        var tenant=new TenantState();var tenantId=Guid.NewGuid();tenant.SetTenant(tenantId);
        var db=new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,tenant);
        var plan=new Plan{Id=Guid.NewGuid(),TenantId=tenantId,Name="Monthly",Price=100,DurationDays=30};
        var sub=new Subscription{Id=Guid.NewGuid(),TenantId=tenantId,ClientId=Guid.NewGuid(),PlanId=plan.Id,Status=SubscriptionStatus.Pending,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow,SnapshotPrice=100,SnapshotDurationDays=30};
        db.Plans.Add(plan);db.Subscriptions.Add(sub);await db.SaveChangesAsync();plan.Price=200;plan.DurationDays=60;await db.SaveChangesAsync();
        Assert.Equal(100,sub.SnapshotPrice);Assert.Equal(30,sub.SnapshotDurationDays);
    }

    [Fact]
    public async Task InstaPayConfirmationTwice_ConflictsAndActivatesOnce()
    {
        await using var f=await PaymentFixture.Create(UserRole.Owner);
        var controller=f.Controller();
        Assert.IsType<OkObjectResult>(await controller.Confirm(f.Payment.Id,CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.Confirm(f.Payment.Id,CancellationToken.None));
        Assert.Equal(SubscriptionStatus.Active,f.Subscription.Status);
        Assert.Single(await f.Db.SecurityAuditLogs.Where(x=>x.EventType=="payment.instapay.confirmed").ToListAsync());
    }

    [Fact]
    public async Task InstaPaySubmit_IdempotencyReplaysSamePaymentAndRejectsChangedBody()
    {
        await using var f=await PaymentFixture.Create(UserRole.Client);
        f.Db.Payments.Remove(f.Payment);await f.Db.SaveChangesAsync();
        f.Http.Request.Headers["Idempotency-Key"]="submit-1";
        var request=new InstaPaySubmitRequest(f.Subscription.Id,"REF-ONE",f.Receipt.Id,125m);
        var first=Assert.IsType<ObjectResult>(await f.Controller().SubmitInstaPay(request,CancellationToken.None));
        Assert.Equal(201,first.StatusCode);
        var replay=Assert.IsType<ContentResult>(await f.Controller().SubmitInstaPay(request,CancellationToken.None));
        Assert.Equal(201,replay.StatusCode);
        Assert.Single(await f.Db.Payments.ToListAsync());
        var changed=Assert.IsType<UnprocessableEntityObjectResult>(await f.Controller().SubmitInstaPay(request with { ReferenceNumber="REF-TWO" },CancellationToken.None));
        Assert.Equal(422,changed.StatusCode);
    }

    [Fact]
    public async Task ClientCannotPollAnotherClientsPayment()
    {
        await using var f=await PaymentFixture.Create(UserRole.Client);
        var controller=f.Controller();
        f.Http.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()),new Claim("tenantId",f.TenantId.ToString()),new Claim(ClaimTypes.Role,UserRole.Client.ToString())},"test"));
        Assert.IsType<NotFoundResult>(await controller.Status(f.Payment.Id,CancellationToken.None));
    }

    [Fact]
    public async Task ManualCashPayment_RequiresIdempotencyAndActivatesOnce()
    {
        await using var f=await PaymentFixture.Create(UserRole.Owner);var controller=f.Controller();f.Http.Request.Headers["Idempotency-Key"]="cash-once";
        var request=new ManualPaymentRequest(f.Subscription.Id,125m);
        var first=Assert.IsType<ObjectResult>(await controller.RegisterManualPayment(request,CancellationToken.None));Assert.Equal(201,first.StatusCode);
        var replay=Assert.IsType<ContentResult>(await controller.RegisterManualPayment(request,CancellationToken.None));Assert.Equal(201,replay.StatusCode);
        Assert.Equal(SubscriptionStatus.Active,f.Subscription.Status);
        Assert.Single(await f.Db.Payments.Where(x=>x.Method==PaymentMethod.Cash).ToListAsync());
        Assert.Single(await f.Db.SecurityAuditLogs.Where(x=>x.EventType=="payment.manual.confirmed").ToListAsync());
    }

    [Fact]
    public async Task Invoice_ConfirmedOnlyOwnedAndSequentialPerTenant()
    {
        await using var f=await PaymentFixture.Create(UserRole.Client);var controller=f.Controller();
        Assert.IsType<ConflictObjectResult>(await controller.Invoice(f.Payment.Id,CancellationToken.None));
        f.Payment.Status=PaymentStatus.Confirmed;await f.Db.SaveChangesAsync();
        var first=Assert.IsType<FileContentResult>(await controller.Invoice(f.Payment.Id,CancellationToken.None));
        var second=Assert.IsType<FileContentResult>(await controller.Invoice(f.Payment.Id,CancellationToken.None));
        Assert.Equal("application/pdf",first.ContentType);Assert.StartsWith("%PDF-1.4",System.Text.Encoding.ASCII.GetString(first.FileContents));
        Assert.Equal(first.FileDownloadName,second.FileDownloadName);var sequence=await f.Db.InvoiceSequences.SingleAsync(x=>x.TenantId==f.TenantId);Assert.Equal(2,sequence.NextNumber);
    }

    private sealed class TenantState : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; }
        public void SetTenant(Guid tenantId)=>TenantId=tenantId;
        public void Clear()=>TenantId=null;
    }

    private sealed class PaymentFixture : IAsyncDisposable
    {
        private readonly Guid _userId=Guid.NewGuid();
        private PaymentFixture(CoachPulseDbContext db,TenantState tenant,Guid tenantId,Payment payment,Subscription subscription,StoredFile receipt,User user,Client client,Plan plan,Microsoft.AspNetCore.Http.DefaultHttpContext http)
        {Db=db;Tenant=tenant;TenantId=tenantId;Payment=payment;Subscription=subscription;Receipt=receipt;User=user;Client=client;Plan=plan;Http=http;}
        public CoachPulseDbContext Db{get;} public TenantState Tenant{get;} public Guid TenantId{get;} public Payment Payment{get;} public Subscription Subscription{get;} public StoredFile Receipt{get;} public User User{get;} public Client Client{get;} public Plan Plan{get;} public DefaultHttpContext Http{get;}
        public static async Task<PaymentFixture> Create(UserRole role)
        {
            var tenantId=Guid.NewGuid();var tenant=new TenantState();tenant.SetTenant(tenantId);var db=new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,tenant);await db.Database.EnsureCreatedAsync();
            var user=new User{Id=Guid.NewGuid(),TenantId=tenantId,Email="client@test.local",Role=role};var client=new Client{Id=Guid.NewGuid(),TenantId=tenantId,UserId=user.Id,User=user};var plan=new Plan{Id=Guid.NewGuid(),TenantId=tenantId,Name="Test",Price=125,DurationDays=30};var sub=new Subscription{Id=Guid.NewGuid(),TenantId=tenantId,ClientId=client.Id,Client=client,PlanId=plan.Id,Plan=plan,Status=SubscriptionStatus.Pending,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow,SnapshotPrice=125,SnapshotDurationDays=30};var payment=new Payment{Id=Guid.NewGuid(),TenantId=tenantId,SubscriptionId=sub.Id,Subscription=sub,Amount=125,Currency="EGP",Method=PaymentMethod.InstaPay,Status=PaymentStatus.PendingVerification};var receipt=new StoredFile{Id=Guid.NewGuid(),TenantId=tenantId,OwnerUserId=user.Id,Purpose="receipt",OriginalName="receipt.png",ContentType="image/png",StoragePath="private",Sha256="a",CreatedAt=DateTime.UtcNow};
            db.Tenants.Add(new Tenant{Id=tenantId,Name="Tenant",Slug=Guid.NewGuid().ToString("N"),Status="Active",InstaPayEnabled=true});db.Users.Add(user);db.Clients.Add(client);db.Plans.Add(plan);db.Subscriptions.Add(sub);db.Payments.Add(payment);db.StoredFiles.Add(receipt);await db.SaveChangesAsync();return new(db,tenant,tenantId,payment,sub,receipt,user,client,plan,new DefaultHttpContext());
        }
        public PaymentsController Controller()
        {
            Http.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,User.Id.ToString()),new Claim("tenantId",TenantId.ToString()),new Claim(ClaimTypes.Role,User.Role.ToString())},"test"));
            return new PaymentsController(Db,new FakePaymob(),new SubscriptionActivationService(),new SubscriptionActivationService(),new FakeHubContext()){ControllerContext=new ControllerContext{HttpContext=Http}};
        }
        public async ValueTask DisposeAsync()=>await Db.DisposeAsync();
    }

    private sealed class FakePaymob : IPaymobClient
    {
        public Task<PaymobIntentionResult> CreatePaymentIntentionAsync(decimal amount,string currency,string orderId,string billingEmail,string billingFirstName,string billingLastName,string billingPhone,CancellationToken cancellationToken)=>Task.FromResult(new PaymobIntentionResult("client-secret",orderId));
        public Task<string> RefundAsync(string transactionId,decimal amount,CancellationToken cancellationToken)=>Task.FromResult("ok");
    }
    private sealed class FakeHubContext : IHubContext<NotificationsHub>
    {
        public IHubClients Clients{get;}=new FakeClients();public IGroupManager Groups{get;}=new FakeGroups();
    }
    private sealed class FakeClients : IHubClients
    {
        private static readonly IClientProxy Proxy=new FakeClientProxy();
        public IClientProxy All=>Proxy;public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)=>Proxy;public IClientProxy Client(string connectionId)=>Proxy;public IClientProxy Clients(IReadOnlyList<string> connectionIds)=>Proxy;public IClientProxy Group(string groupName)=>Proxy;public IClientProxy GroupExcept(string groupName,IReadOnlyList<string> excludedConnectionIds)=>Proxy;public IClientProxy Groups(IReadOnlyList<string> groupNames)=>Proxy;public IClientProxy User(string userId)=>Proxy;public IClientProxy Users(IReadOnlyList<string> userIds)=>Proxy;
    }
    private sealed class FakeClientProxy : IClientProxy { public Task SendCoreAsync(string method,object?[] args,CancellationToken cancellationToken=default)=>Task.CompletedTask; }
    private sealed class FakeGroups : IGroupManager { public Task AddToGroupAsync(string connectionId,string groupName,CancellationToken cancellationToken=default)=>Task.CompletedTask;public Task RemoveFromGroupAsync(string connectionId,string groupName,CancellationToken cancellationToken=default)=>Task.CompletedTask; }
}
