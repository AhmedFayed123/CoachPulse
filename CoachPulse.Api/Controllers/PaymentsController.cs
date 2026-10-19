using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoachPulse.Api.Hubs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Application.DTOs;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using CoachPulse.Application.DTOs.Payments;

namespace CoachPulse.Api.Controllers;

[ApiController, Route("api/payments"), Authorize]
public sealed class PaymentsController(CoachPulseDbContext db, IPaymobClient paymob, ISubscriptionActivationService activation,
    ISubscriptionStateMachine states, IHubContext<NotificationsHub> hub, IDataProtectionProvider dataProtectionProvider,
    INotificationService? notificationService = null) : ControllerBase
{
    private readonly IDataProtector _snapshotProtector = dataProtectionProvider.CreateProtector("CoachPulse.Idempotency.ResponseSnapshot.v1");

    [HttpPost("paymob/create-intention"), Authorize(Policy="ClientOnly")]
    [ProducesResponseType(typeof(PaymobCheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePaymobIntention([FromBody] CreatePaymentRequest req,CancellationToken ct)
    {
        if(!Identity(out var tenant,out var user,out _))return Unauthorized();var key=Request.Headers["Idempotency-Key"].ToString();if(string.IsNullOrWhiteSpace(key))return BadRequest("Idempotency-Key is required.");
        var replay=await Replay(key,tenant,user,"paymob.create-intention",req,ct);if(replay is not null)return replay;
        var client=await db.Clients.Include(x=>x.User).FirstOrDefaultAsync(x=>x.TenantId==tenant&&x.UserId==user,ct);if(client is null)return NotFound();
        var plan=await db.Plans.FirstOrDefaultAsync(x=>x.Id==req.PlanId&&x.TenantId==tenant&&x.IsActive,ct);if(plan is null)return NotFound();
        var t=await db.Tenants.FirstOrDefaultAsync(x=>x.Id==tenant,ct);if(t is null)return NotFound();if(!t.PaymobEnabled)return Conflict("Paymob is disabled for this tenant.");
        var sub=await db.Subscriptions.FirstOrDefaultAsync(x=>x.TenantId==tenant&&x.ClientId==client.Id&&x.PlanId==plan.Id&&x.Status==SubscriptionStatus.Pending,ct);
        if(sub is null){if(await db.Subscriptions.AnyAsync(x=>x.TenantId==tenant&&x.ClientId==client.Id&&x.PlanId==plan.Id&&x.Status==SubscriptionStatus.Active,ct))return Conflict("An active subscription already exists.");sub=new(){Id=Guid.NewGuid(),TenantId=tenant,ClientId=client.Id,PlanId=plan.Id,Status=SubscriptionStatus.Pending,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow,SnapshotPrice=plan.Price,SnapshotDurationDays=plan.DurationDays};db.Subscriptions.Add(sub);}
        var order=Guid.NewGuid().ToString("N");var payment=new Payment{Id=Guid.NewGuid(),TenantId=tenant,SubscriptionId=sub.Id,Amount=sub.SnapshotPrice,Currency=t.Currency,Method=PaymentMethod.Paymob,Status=PaymentStatus.Pending,CreatedAt=DateTime.UtcNow};db.Payments.Add(payment);
        try
        {
            var intention=await paymob.CreatePaymentIntentionAsync(payment.Amount,payment.Currency,order,client.User.Email,client.User.Name??"Client","",client.User.Phone??"+200000000000",ct);
            payment.GatewayReference=intention.OrderId;
            var result=new PaymobCheckoutResponse(payment.Id,sub.Id,intention.CheckoutUrl,intention.OrderId,payment.Amount,payment.Currency);
            SaveKey(key,tenant,user,"paymob.create-intention",req,200,result);
            await db.SaveChangesAsync(ct);
            return Ok(result);
        }
        catch(HttpRequestException){return StatusCode(502,new{message="Payment provider unavailable."});}
    }

    [HttpGet("{id:guid}/status"),Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Status(Guid id,CancellationToken ct){if(!Identity(out var tenant,out var user,out _))return Unauthorized();var p=await db.Payments.AsNoTracking().Where(x=>x.Id==id&&x.TenantId==tenant&&x.Subscription.Client.UserId==user).Select(x=>new{x.Status,x.SubscriptionId}).FirstOrDefaultAsync(ct);return p is null?NotFound():Ok(p);}
    [HttpGet("instapay/instructions"),Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Instructions(CancellationToken ct){if(!Identity(out var tenant,out _,out _))return Unauthorized();var t=await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==tenant,ct);if(t is null)return NotFound();return !t.InstaPayEnabled?Conflict("InstaPay is disabled."):Ok(new{t.InstaPayId,t.InstaPayAccountNumber,t.Currency});}

    [HttpPost("instapay/submit"),Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> SubmitInstaPay([FromBody] InstaPaySubmitRequest req,CancellationToken ct)
    {
        if(!Identity(out var tenant,out var user,out _))return Unauthorized();var key=Request.Headers["Idempotency-Key"].ToString();if(string.IsNullOrWhiteSpace(key))return BadRequest("Idempotency-Key is required.");var replay=await Replay(key,tenant,user,"instapay.submit",req,ct);if(replay is not null)return replay;
        var t=await db.Tenants.FirstOrDefaultAsync(x=>x.Id==tenant,ct);if(t is null)return NotFound();if(!t.InstaPayEnabled)return Conflict("InstaPay is disabled.");
        var sub=await db.Subscriptions.Include(x=>x.Client).FirstOrDefaultAsync(x=>x.Id==req.SubscriptionId&&x.TenantId==tenant&&x.Client.UserId==user,ct);if(sub is null)return NotFound();if(sub.Status!=SubscriptionStatus.Pending)return Conflict("Subscription must be pending.");
        if(req.Amount!=sub.SnapshotPrice)return UnprocessableEntity("Amount does not match subscription price.");if(string.IsNullOrWhiteSpace(req.ReferenceNumber))return UnprocessableEntity();
        if(await db.Payments.AnyAsync(x=>x.TenantId==tenant&&x.ReferenceNumber==req.ReferenceNumber,ct))return Conflict("Reference number already submitted.");
        var receipt=await db.StoredFiles.FirstOrDefaultAsync(x=>x.Id==req.ReceiptFileId&&x.TenantId==tenant&&x.OwnerUserId==user&&!x.IsDeleted&&x.Purpose=="receipt",ct);if(receipt is null)return NotFound();
        var p=new Payment{Id=Guid.NewGuid(),TenantId=tenant,SubscriptionId=sub.Id,Amount=req.Amount,Currency=t.Currency,Method=PaymentMethod.InstaPay,Status=PaymentStatus.PendingVerification,ReferenceNumber=req.ReferenceNumber.Trim(),ReceiptFileId=receipt.Id,CreatedAt=DateTime.UtcNow};receipt.LinkedEntityType="PaymentReceipt";receipt.LinkedEntityId=p.Id;db.Payments.Add(p);
        var result=new{p.Id,p.SubscriptionId,p.Amount,p.Currency,p.Status};SaveKey(key,tenant,user,"instapay.submit",req,201,result);try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){return Conflict("Payment reference or idempotency key was already used.");}
        var owners=await db.Users.IgnoreQueryFilters().Where(x=>x.TenantId==tenant&&x.Role==UserRole.TenantOwner&&!x.IsDeleted).Select(x=>x.Id).ToListAsync(ct);foreach(var owner in owners)await Notify(owner,tenant,"PendingPaymentCreated","تحويل دفع بانتظار المراجعة","يوجد تحويل دفع بانتظار المراجعة.",new{paymentId=p.Id,subscriptionId=p.SubscriptionId,amount=p.Amount,currency=p.Currency},ct);
        return StatusCode(201,result);
    }

    [HttpPost("~/api/tenant/payments/manual"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> RegisterManualPayment([FromBody] ManualPaymentRequest req, CancellationToken ct)
    {
        if (!Identity(out var tenant, out var owner, out _)) return Unauthorized();
        var key = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key)) return BadRequest("Idempotency-Key is required.");
        if (req.Amount <= 0) return UnprocessableEntity("Amount must be positive.");
        var replay = await Replay(key, tenant, owner, "payment.manual", req, ct);
        if (replay is not null) return replay;

        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({req.SubscriptionId.ToString()}, 0))", ct);
        var afterLock = await Replay(key, tenant, owner, "payment.manual", req, ct);
        if (afterLock is not null) return afterLock;
        var subscription = await db.Subscriptions.Include(x => x.Plan).Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == req.SubscriptionId && x.TenantId == tenant, ct);
        if (subscription is null) return NotFound();
        if (subscription.Status != SubscriptionStatus.Pending) return Conflict("Only a pending subscription can receive a manual payment.");
        if (req.Amount != subscription.SnapshotPrice) return UnprocessableEntity("Amount must match the subscription snapshot price.");
        var tenantSettings = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenant, ct);
        if (tenantSettings is null) return NotFound();
        var payment = new Payment { Id = Guid.NewGuid(), TenantId = tenant, SubscriptionId = subscription.Id,
            Amount = req.Amount, Currency = tenantSettings.Currency, Method = PaymentMethod.Cash,
            Status = PaymentStatus.Confirmed, ConfirmedByAdminId = owner, CreatedAt = DateTime.UtcNow };
        try
        {
            await activation.ActivateAsync(subscription, SubscriptionActivationSource.OwnerManual, ct);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        db.Payments.Add(payment);
        Audit(owner, tenant, "payment.manual.confirmed", $"payment={payment.Id};subscription={subscription.Id};amount={payment.Amount}");
        var response = new { paymentId = payment.Id, subscriptionId = subscription.Id, payment.Method, paymentStatus = payment.Status, subscriptionStatus = subscription.Status };
        SaveKey(key, tenant, owner, "payment.manual", req, 201, response);
        try { await db.SaveChangesAsync(ct); if (tx is not null) await tx.CommitAsync(ct); }
        catch (DbUpdateException) { if (tx is not null) await tx.RollbackAsync(ct); return Conflict("Payment or idempotency key was already processed."); }
        await Notify(subscription.Client.UserId, tenant, "PaymentStatusChanged", "Payment confirmed", "Your cash payment was recorded.", new { paymentId = payment.Id, subscriptionId = subscription.Id, status = payment.Status }, ct);
        await Notify(subscription.Client.UserId, tenant, "SubscriptionStatusChanged", "Subscription activated", "Your subscription is active.", new { subscriptionId = subscription.Id, status = subscription.Status }, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("~/api/my/payments/{id:guid}/invoice"), Authorize(Policy = "ClientOnly")]
    public async Task<IActionResult> Invoice(Guid id, CancellationToken ct)
    {
        if (!Identity(out var tenant, out var user, out _)) return Unauthorized();
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({tenant.ToString()}, 0))", ct);
        var payment = await db.Payments.Include(x => x.Subscription).ThenInclude(x => x.Client).Include(x => x.Subscription).ThenInclude(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant && x.Subscription.Client.UserId == user, ct);
        if (payment is null) return NotFound();
        if (payment.Status != PaymentStatus.Confirmed) return Conflict("An invoice is available only for confirmed payments.");
        if (payment.InvoiceNumber is null)
        {
            var sequence = await db.InvoiceSequences.FirstOrDefaultAsync(x => x.TenantId == tenant, ct);
            if (sequence is null)
            {
                sequence = new InvoiceSequence { TenantId = tenant, NextNumber = 2 };
                payment.InvoiceNumber = 1;
                db.InvoiceSequences.Add(sequence);
            }
            else { payment.InvoiceNumber = sequence.NextNumber; sequence.NextNumber++; }
            try { await db.SaveChangesAsync(ct); if (tx is not null) await tx.CommitAsync(ct); }
            catch (DbUpdateException) { if (tx is not null) await tx.RollbackAsync(ct); return Conflict("Invoice allocation raced. Retry the request."); }
        }
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentDisposition = $"attachment; filename=\"invoice-{payment.InvoiceNumber:D8}.pdf\"";
        var tenantInfo = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenant, ct);
        var pdf = InvoicePdf.Create(new[] { "CoachPulse receipt", $"Invoice: {payment.InvoiceNumber:D8}", $"Date: {payment.CreatedAt:yyyy-MM-dd}",
            $"Tenant: {tenantInfo?.Name ?? "Fitness provider"}", $"Plan: {payment.Subscription.Plan.Name}", $"Amount paid: {payment.Amount:0.00} {payment.Currency}", "Status: Paid" });
        return File(pdf, "application/pdf");
    }

    [HttpGet("~/api/my/payments"),Authorize(Policy="ClientOnly")]
    [ProducesResponseType(typeof(PagedResult<PaymentListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyPayments(CancellationToken ct){if(!Identity(out var tenant,out var user,out _))return Unauthorized();return Ok(await db.Payments.AsNoTracking().Where(x=>x.TenantId==tenant&&x.Subscription.Client.UserId==user).OrderByDescending(x=>x.CreatedAt).Select(x=>PaymentDto(x)).ToListAsync(ct));}
    [HttpGet("~/api/my/payments/{id:guid}"),Authorize(Policy="ClientOnly")]
    [ProducesResponseType(typeof(PaymentListItem), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyPayment(Guid id,CancellationToken ct){if(!Identity(out var tenant,out var user,out _))return Unauthorized();var x=await db.Payments.AsNoTracking().Where(x=>x.Id==id&&x.TenantId==tenant&&x.Subscription.Client.UserId==user).Select(x=>PaymentDto(x)).FirstOrDefaultAsync(ct);return x is null?NotFound():Ok(x);}

    [HttpGet("~/api/tenant/payments"),Authorize(Policy="CoachOnly")]
    [ProducesResponseType(typeof(PagedResult<PaymentListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TenantPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)=>await PaymentList(false,page,pageSize,ct);
    [HttpGet("~/api/tenant/payments/pending"),Authorize(Policy="CoachOnly")]
    [ProducesResponseType(typeof(PagedResult<PaymentListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pending([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)=>await PaymentList(true,page,pageSize,ct);
    [HttpGet("~/api/tenant/payments/{id:guid}"),Authorize(Policy="CoachOnly")]
    [ProducesResponseType(typeof(PaymentListItem), StatusCodes.Status200OK)]
    public async Task<IActionResult> TenantPayment(Guid id,CancellationToken ct)
    {
        if(!Identity(out var tenant,out var user,out var role))return Unauthorized();var q=db.Payments.AsNoTracking().Include(x=>x.Subscription).ThenInclude(x=>x.Client).Where(x=>x.Id==id&&x.TenantId==tenant);if(role==UserRole.CoachStaff)q=q.Where(x=>x.Subscription.Client.CoachId==user);var p=await q.Select(x=>PaymentDto(x)).FirstOrDefaultAsync(ct);return p is null?NotFound():Ok(p);
    }
    [HttpPut("instapay/{id:guid}/confirm"),Authorize(Policy="OwnerOnly")]
    public async Task<IActionResult> Confirm(Guid id,CancellationToken ct)
    {
        if(!Identity(out var tenant,out var owner,out _))return Unauthorized();await using var tx=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(ct):null;var p=await db.Payments.Include(x=>x.Subscription).ThenInclude(x=>x.Client).Include(x=>x.Subscription).ThenInclude(x=>x.Plan).FirstOrDefaultAsync(x=>x.Id==id&&x.TenantId==tenant,ct);if(p is null)return NotFound();if(p.Method!=PaymentMethod.InstaPay||p.Status!=PaymentStatus.PendingVerification)return Conflict("Payment is no longer pending verification.");
        p.Status=PaymentStatus.Confirmed;p.Version=Guid.NewGuid().ToByteArray();p.ConfirmedByAdminId=owner;
        try{await activation.ActivateAsync(p.Subscription,SubscriptionActivationSource.InstaPayConfirmation,ct);Audit(owner,tenant,"payment.instapay.confirmed",$"payment={p.Id};amount={p.Amount}");await db.SaveChangesAsync(ct);if(tx is not null)await tx.CommitAsync(ct);}catch(DbUpdateConcurrencyException){if(tx is not null)await tx.RollbackAsync(ct);return Conflict();}catch(InvalidOperationException ex){if(tx is not null)await tx.RollbackAsync(ct);return Conflict(ex.Message);}
        await Notify(p.Subscription.Client.UserId,tenant,"PaymentStatusChanged","حالة الدفع","تم تحديث حالة دفعتك.",new{paymentId=p.Id,status=p.Status,subscriptionId=p.SubscriptionId},ct);await Notify(p.Subscription.Client.UserId,tenant,"SubscriptionStatusChanged","حالة الاشتراك","تم تحديث حالة اشتراكك.",new{subscriptionId=p.SubscriptionId,status=p.Subscription.Status},ct);return Ok(new{p.Id,PaymentStatus=p.Status,SubscriptionStatus=p.Subscription.Status});
    }
    [HttpPut("instapay/{id:guid}/reject"),Authorize(Policy="OwnerOnly")]
    public async Task<IActionResult> Reject(Guid id,[FromBody] RejectPaymentRequest req,CancellationToken ct)
    {
        if(!Identity(out var tenant,out var owner,out _))return Unauthorized();if(string.IsNullOrWhiteSpace(req.Reason))return UnprocessableEntity("Reason is required.");var p=await db.Payments.Include(x=>x.Subscription).ThenInclude(x=>x.Client).FirstOrDefaultAsync(x=>x.Id==id&&x.TenantId==tenant,ct);if(p is null)return NotFound();if(p.Method!=PaymentMethod.InstaPay||p.Status!=PaymentStatus.PendingVerification)return Conflict();p.Status=PaymentStatus.Rejected;p.RejectionReason=req.Reason.Trim();Audit(owner,tenant,"payment.instapay.rejected",$"payment={id};amount={p.Amount};reason={req.Reason}");await db.SaveChangesAsync(ct);await Notify(p.Subscription.Client.UserId,tenant,"PaymentStatusChanged","حالة الدفع","تم تحديث حالة دفعتك.",new{paymentId=p.Id,status=p.Status,subscriptionId=p.SubscriptionId},ct);return Ok(new{p.Id,p.Status});
    }

    [HttpPost("{id:guid}/refund"),Authorize(Policy="OwnerOnly")]
    public async Task<IActionResult> Refund(Guid id,[FromBody] RefundRequest req,CancellationToken ct)
    {
        if(!Identity(out var tenant,out var owner,out _))return Unauthorized();var key=Request.Headers["Idempotency-Key"].ToString();if(string.IsNullOrWhiteSpace(key))return BadRequest("Idempotency-Key is required.");var endpoint=$"payment.refund/{id}";var replay=await Replay(key,tenant,owner,endpoint,req,ct);if(replay is not null)return replay;
        await using var tx=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(ct):null;
        if(db.Database.IsNpgsql())await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))",ct);
        var afterLock=await Replay(key,tenant,owner,endpoint,req,ct);if(afterLock is not null)return afterLock;
        var p=await db.Payments.Include(x=>x.Subscription).ThenInclude(x=>x.Plan).Include(x=>x.Subscription).ThenInclude(x=>x.Client).FirstOrDefaultAsync(x=>x.Id==id&&x.TenantId==tenant,ct);if(p is null)return NotFound();var refunded=await db.PaymentRefunds.Where(x=>x.PaymentId==id&&x.TenantId==tenant).SumAsync(x=>(decimal?)x.Amount,ct)??0m;var remaining=p.Amount-refunded;if(req.Amount<=0||req.Amount>remaining)return UnprocessableEntity("Refund exceeds the refundable balance.");
        string? gateway=null;if(p.Method==PaymentMethod.Paymob){if(string.IsNullOrWhiteSpace(p.GatewayTransactionId))return Conflict("Gateway transaction reference is unavailable until Paymob confirms payment.");try{gateway=await paymob.RefundAsync(p.GatewayTransactionId,req.Amount,ct);}catch(HttpRequestException){return StatusCode(502,"Paymob refund failed.");}}
        var r=new PaymentRefund{Id=Guid.NewGuid(),TenantId=tenant,PaymentId=id,Amount=req.Amount,Currency=p.Currency,GatewayReference=gateway,CreatedByUserId=owner,CreatedAt=DateTime.UtcNow};db.PaymentRefunds.Add(r);var total=refunded+req.Amount;p.Status=total==p.Amount?PaymentStatus.Refunded:PaymentStatus.PartiallyRefunded;p.Version=Guid.NewGuid().ToByteArray();
        if(total==p.Amount&&p.Subscription.Status is SubscriptionStatus.Pending or SubscriptionStatus.Active or SubscriptionStatus.Paused){try{states.Transition(p.Subscription,SubscriptionStatus.Cancelled,"Full refund: subscription cancelled immediately.");}catch(InvalidOperationException ex){return Conflict(ex.Message);}}
        Audit(owner,tenant,"payment.refunded",$"payment={id};amount={req.Amount};total={total}");var response=new{r.Id,paymentId=id,r.Amount,p.Currency,p.Status};SaveKey(key,tenant,owner,endpoint,req,201,response);try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){if(tx is not null)await tx.RollbackAsync(ct);return Conflict("Payment changed while refunding; retry with the same Idempotency-Key.");}catch(DbUpdateException){if(tx is not null)await tx.RollbackAsync(ct);return Conflict("Refund or Idempotency-Key was already processed.");}if(tx is not null)await tx.CommitAsync(ct);await Notify(p.Subscription.Client.UserId,tenant,"PaymentStatusChanged","حالة الدفع","تم تحديث حالة دفعتك.",new{paymentId=p.Id,status=p.Status,subscriptionId=p.SubscriptionId},ct);if(total==p.Amount)await Notify(p.Subscription.Client.UserId,tenant,"SubscriptionStatusChanged","حالة الاشتراك","تم تحديث حالة اشتراكك.",new{subscriptionId=p.SubscriptionId,status=p.Subscription.Status},ct);return StatusCode(201,response);
    }

    [HttpGet("~/api/tenant/payments/summary"),Authorize(Policy="CoachOnly")]
    public async Task<IActionResult> Summary([FromQuery] DateTime? from,[FromQuery] DateTime? to,CancellationToken ct){if(!Identity(out var tenant,out _,out _))return Unauthorized();var paymentsQ=db.Payments.AsNoTracking().Where(x=>x.TenantId==tenant);var refundsQ=db.PaymentRefunds.AsNoTracking().Where(x=>x.TenantId==tenant);if(from.HasValue){paymentsQ=paymentsQ.Where(x=>x.CreatedAt>=from);refundsQ=refundsQ.Where(x=>x.CreatedAt>=from);}if(to.HasValue){paymentsQ=paymentsQ.Where(x=>x.CreatedAt<to);refundsQ=refundsQ.Where(x=>x.CreatedAt<to);}var payments=await paymentsQ.Select(x=>new{x.Id,x.Method,x.Amount}).ToListAsync(ct);var refunds=await refundsQ.Select(x=>new{x.PaymentId,x.Amount}).ToListAsync(ct);var methods=payments.ToDictionary(x=>x.Id,x=>x.Method);var refunded=refunds.Where(x=>methods.ContainsKey(x.PaymentId)).GroupBy(x=>methods[x.PaymentId]).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Amount));var rows=payments.GroupBy(x=>x.Method).Select(g=>new{Method=g.Key,Revenue=g.Sum(x=>x.Amount)-(refunded.TryGetValue(g.Key,out var amount)?amount:0m)});return Ok(rows);}
    [HttpGet("~/api/tenant/payments/export"),Authorize(Policy="OwnerOnly")]
    public async Task<IActionResult> Export([FromQuery] DateTime? from,[FromQuery] DateTime? to,CancellationToken ct){if(!Identity(out var tenant,out _,out _))return Unauthorized();var q=db.Payments.AsNoTracking().Where(x=>x.TenantId==tenant);if(from.HasValue)q=q.Where(x=>x.CreatedAt>=from);if(to.HasValue)q=q.Where(x=>x.CreatedAt<to);var rows=await q.Select(x=>new{x.Id,x.CreatedAt,x.Amount,x.Currency,x.Method,x.Status,x.ReferenceNumber}).ToListAsync(ct);var sb=new StringBuilder("Id,CreatedAt,Amount,Currency,Method,Status,ReferenceNumber\r\n");foreach(var p in rows)sb.AppendLine(string.Join(',',new[]{p.Id.ToString(),p.CreatedAt.ToString("O"),p.Amount.ToString(CultureInfo.InvariantCulture),p.Currency,p.Method.ToString(),p.Status.ToString(),p.ReferenceNumber??""}.Select(Csv)));return File(Encoding.UTF8.GetBytes(sb.ToString()),"text/csv","payments.csv");}

    private async Task<IActionResult> PaymentList(bool pending,int page,int pageSize,CancellationToken ct){if(page<1||pageSize is <1 or >100)return UnprocessableEntity("page must be at least 1 and pageSize must be between 1 and 100.");if(!Identity(out var tenant,out var user,out var role))return Unauthorized();var q=db.Payments.AsNoTracking().Where(x=>x.TenantId==tenant);if(pending)q=q.Where(x=>x.Status==PaymentStatus.PendingVerification);if(role==UserRole.CoachStaff)q=q.Where(x=>x.Subscription.Client.CoachId==user);var totalCount=await q.CountAsync(ct);var items=await q.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>PaymentDto(x)).ToListAsync(ct);return Ok(PagedResult<object>.Create(items,page,pageSize,totalCount));}
    private async Task<IActionResult?> Replay(string key,Guid tenant,Guid user,string endpoint,object request,CancellationToken ct){var expired=await db.IdempotencyKeys.Where(x=>x.TenantId==tenant&&x.UserId==user&&x.Endpoint==endpoint&&x.Key==key&&x.CreatedAt<DateTime.UtcNow.AddHours(-24)).ToListAsync(ct);if(expired.Count>0){db.IdempotencyKeys.RemoveRange(expired);await db.SaveChangesAsync(ct);}var hash=Hash(JsonSerializer.Serialize(request,JsonSerializerOptions.Web));var old=await db.IdempotencyKeys.FirstOrDefaultAsync(x=>x.TenantId==tenant&&x.UserId==user&&x.Endpoint==endpoint&&x.Key==key&&x.CreatedAt>=DateTime.UtcNow.AddHours(-24),ct);if(old is null)return null;if(old.RequestHash!=hash)return UnprocessableEntity("Idempotency-Key was used with a different body.");if(!old.ResponseSnapshot.StartsWith("dp1:",StringComparison.Ordinal)){db.IdempotencyKeys.Remove(old);await db.SaveChangesAsync(ct);return Conflict("The stored idempotency response predates protected snapshots. Retry with a new Idempotency-Key.");}try{var protectedJson=old.ResponseSnapshot[4..];var s=JsonSerializer.Deserialize<Snapshot>(_snapshotProtector.Unprotect(protectedJson),JsonSerializerOptions.Web)!;return new ContentResult{StatusCode=s.Status,ContentType="application/json",Content=s.Body};}catch(CryptographicException){return Conflict("The protected idempotency response is unavailable. Retry with a new Idempotency-Key.");}}
    private void SaveKey(string key,Guid tenant,Guid user,string endpoint,object request,int status,object body){var snapshot=JsonSerializer.Serialize(new Snapshot(status,JsonSerializer.Serialize(body,JsonSerializerOptions.Web)),JsonSerializerOptions.Web);db.IdempotencyKeys.Add(new(){Id=Guid.NewGuid(),TenantId=tenant,Key=key,UserId=user,Endpoint=endpoint,RequestHash=Hash(JsonSerializer.Serialize(request,JsonSerializerOptions.Web)),ResponseSnapshot="dp1:"+_snapshotProtector.Protect(snapshot),CreatedAt=DateTime.UtcNow});}
    private static string Hash(string s)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
    private static string Csv(string s)=>"\""+(s.Length>0&&s[0] is '=' or '+' or '-' or '@'?"'":"")+s.Replace("\"","\"\"")+"\"";
    private static PaymentListItem PaymentDto(Payment x)=>new(x.Id,x.SubscriptionId,x.Amount,x.Currency,x.Method,x.Status,x.ReferenceNumber,x.CreatedAt,x.RejectionReason,x.ReceiptFileId);
    private bool Identity(out Guid tenant,out Guid user,out UserRole role){tenant=Guid.Empty;user=Guid.Empty;role=default;return Guid.TryParse(User.FindFirst("tenantId")?.Value,out tenant)&&Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value,out user)&&Enum.TryParse(User.FindFirst(ClaimTypes.Role)?.Value,out role);}
    private void Audit(Guid user,Guid tenant,string type,string id)=>db.SecurityAuditLogs.Add(new(){Id=Guid.NewGuid(),UserId=user,TenantId=tenant,EventType=type,Identifier=id,CreatedAt=DateTime.UtcNow});
    private Task Notify(Guid user,Guid tenant,string type,string title,string body,object data,CancellationToken ct)=>notificationService is null ? hub.Clients.User(user.ToString()).SendAsync(type,data,ct) : notificationService.NotifyAsync(user,tenant,type,title,body,JsonSerializer.Serialize(data),ct);
    private sealed record Snapshot(int Status,string Body);
}
public sealed record CreatePaymentRequest(Guid PlanId, decimal Amount=0m);
public sealed record InstaPaySubmitRequest(Guid SubscriptionId, string ReferenceNumber, Guid ReceiptFileId, decimal Amount);
public sealed record RejectPaymentRequest(string Reason);
public sealed record RefundRequest(decimal Amount,string? Reason);
public sealed record ManualPaymentRequest(Guid SubscriptionId, decimal Amount);

internal static class InvoicePdf
{
    public static byte[] Create(IEnumerable<string> lines)
    {
        static string Safe(string value) => new string(value.Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray()).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var text = "BT /F1 12 Tf 50 790 Td " + string.Join(" T* ", lines.Select(x => $"({Safe(x)}) Tj")) + " ET";
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", $"<< /Length {Encoding.ASCII.GetByteCount(text)} >>\nstream\n{text}\nendstream" };
        var output = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(Encoding.ASCII.GetByteCount(output.ToString())); output.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = Encoding.ASCII.GetByteCount(output.ToString()); output.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) output.Append($"{offset:D10} 00000 n \n");
        output.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(output.ToString());
    }
}
