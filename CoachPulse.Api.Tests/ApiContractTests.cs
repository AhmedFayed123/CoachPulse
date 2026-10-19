using System.Security.Claims;
using System.Text.Json;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Api.Controllers;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Settings;
using CoachPulse.Application.DTOs.Auth;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using CoachPulse.Infrastructure.Persistence;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public void EnumsSerializeAsCanonicalStrings()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonSerialization.AddStringEnums(options);

        Assert.Equal("\"TenantOwner\"", JsonSerializer.Serialize(UserRole.TenantOwner, options));
        Assert.Equal("\"CoachStaff\"", JsonSerializer.Serialize(UserRole.CoachStaff, options));
        Assert.Equal("\"Paymob\"", JsonSerializer.Serialize(PaymentMethod.Paymob, options));
        Assert.Equal("\"InstaPay\"", JsonSerializer.Serialize(PaymentMethod.InstaPay, options));
        Assert.Equal("\"Cash\"", JsonSerializer.Serialize(PaymentMethod.Cash, options));
        Assert.Equal("\"Active\"", JsonSerializer.Serialize(SubscriptionStatus.Active, options));
        Assert.Equal("\"PendingVerification\"", JsonSerializer.Serialize(PaymentStatus.PendingVerification, options));
        Assert.Equal("\"Archived\"", JsonSerializer.Serialize(ClientStatus.Archived, options));
        Assert.Equal("\"BiWeekly\"", JsonSerializer.Serialize(CheckInFrequency.BiWeekly, options));
        Assert.Equal(1, (int)PaymentMethod.Paymob);
    }

    [Theory]
    [InlineData(UserRole.TenantOwner, "TenantOwner")]
    [InlineData(UserRole.CoachStaff, "CoachStaff")]
    [InlineData(UserRole.Client, "Client")]
    [InlineData(UserRole.SuperAdmin, "SuperAdmin")]
    public void JwtRoleClaimUsesCanonicalRoleName(UserRole role, string expected)
    {
        var jwt = new JwtService(Microsoft.Extensions.Options.Options.Create(new JwtSettings
        {
            Key = new string('k', 64), Issuer = "issuer", Audience = "audience", AccessTokenMinutes = 15
        }));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(jwt.GenerateAccessToken(new User
        {
            Id = Guid.NewGuid(), Email = "user@example.test", Role = role
        }));

        Assert.Equal(expected, token.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void AuthResponseSerializesThePublishedCamelCaseShape()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonSerialization.AddStringEnums(options);
        var response = new AuthSessionResponse("access", "refresh", 900,
            new AuthSessionUser(Guid.Empty, "Coach", UserRole.TenantOwner),
            new AuthSessionTenant(Guid.Empty, "studio", "Studio"));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
        Assert.Equal(new[] { "accessToken", "refreshToken", "expiresIn", "user", "tenant" }, json.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(new[] { "id", "name", "role" }, json.RootElement.GetProperty("user").EnumerateObject().Select(p => p.Name));
        Assert.Equal("TenantOwner", json.RootElement.GetProperty("user").GetProperty("role").GetString());
        Assert.Equal(new[] { "id", "slug", "name" }, json.RootElement.GetProperty("tenant").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task ListResultsUseSharedPagedEnvelopeAndQueryPagination()
    {
        var http = new DefaultHttpContext();
        http.TraceIdentifier = "trace-pagination";
        http.Request.QueryString = new QueryString("?page=2&pageSize=2");
        var action = new ActionContext(http, new RouteData(), new ControllerActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var executing = new ResultExecutingContext(action, filters, new OkObjectResult(new[] { "a", "b", "c", "d", "e" }), null!);

        await new ProblemDetailsResultFilter().OnResultExecutionAsync(executing,
            () => Task.FromResult(new ResultExecutedContext(action, filters, executing.Result, null!)));

        var paged = Assert.IsType<PagedResult<object?>>(Assert.IsAssignableFrom<ObjectResult>(executing.Result).Value);
        Assert.Equal(2, paged.Page);
        Assert.Equal(2, paged.PageSize);
        Assert.Equal(5, paged.TotalCount);
        Assert.Equal(new object?[] { "c", "d" }, paged.Items);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(paged, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(new[] { "items", "page", "pageSize", "totalCount" }, json.RootElement.EnumerateObject().Select(x => x.Name));

        http.Request.QueryString = new QueryString("?page=2&pageSize=20");
        var legacy = new ResultExecutingContext(action, filters,
            new OkObjectResult(new { page = 2, limit = 25, total = 45, items = new[] { "legacy" } }), null!);
        await new ProblemDetailsResultFilter().OnResultExecutionAsync(legacy,
            () => Task.FromResult(new ResultExecutedContext(action, filters, legacy.Result, null!)));
        var normalized = Assert.IsType<PagedResult<object?>>(Assert.IsAssignableFrom<ObjectResult>(legacy.Result).Value);
        Assert.Equal(20, normalized.PageSize);
        Assert.Equal(45, normalized.TotalCount);
    }

    [Fact]
    public async Task StringErrorsBecomeProblemDetailsWithTraceId()
    {
        var http = new DefaultHttpContext();
        http.TraceIdentifier = "trace-error";
        var action = new ActionContext(http, new RouteData(), new ControllerActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var executing = new ResultExecutingContext(action, filters, new ConflictObjectResult("conflict text"), null!);

        await new ProblemDetailsResultFilter().OnResultExecutionAsync(executing,
            () => Task.FromResult(new ResultExecutedContext(action, filters, executing.Result, null!)));

        var problem = Assert.IsType<ProblemDetails>(Assert.IsAssignableFrom<ObjectResult>(executing.Result).Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("conflict text", problem.Detail);
        Assert.Equal("trace-error", problem.Extensions["traceId"]);
    }

    [Fact]
    public async Task DashboardUsesCanonicalFieldsAndHidesRevenueFromStaff()
    {
        var tenantId = Guid.NewGuid();
        var state = new TestTenantState(); state.SetTenant(tenantId);
        var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, state);
        await db.Database.EnsureCreatedAsync();
        db.Tenants.Add(new CoachPulse.Domain.Entities.Tenant { Id = tenantId, Name = "Studio", Slug = "studio", Status = "Active" });
        var ownerId = Guid.NewGuid();
        var owner = new User { Id = ownerId, TenantId = tenantId, Email = "owner@example.test", Role = UserRole.TenantOwner };
        var staffId = Guid.NewGuid();
        var staff = new User { Id = staffId, TenantId = tenantId, Email = "staff@example.test", Role = UserRole.CoachStaff };
        var clientUser = new User { Id = Guid.NewGuid(), TenantId = tenantId, Email = "client@example.test", Role = UserRole.Client };
        db.Users.AddRange(owner, staff, clientUser);
        db.Clients.Add(new Client { Id = Guid.NewGuid(), TenantId = tenantId, UserId = clientUser.Id, User = clientUser, CoachId = staffId, Status = ClientStatus.Active });
        await db.SaveChangesAsync();

        var controller = new TenantController(db, new TestPasswordHasher(), new ClientAccessService(db));
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, staffId.ToString()), new Claim("tenantId", tenantId.ToString()), new Claim(ClaimTypes.Role, "CoachStaff")
        ], "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        var response = Assert.IsType<TenantDashboardSummary>(Assert.IsType<OkObjectResult>(await controller.GetDashboardSummaryCanonical()).Value);

        Assert.Equal(1, response.ActiveClients);
        Assert.Null(response.MonthlyRevenue);
        Assert.Null(response.Alerts.PendingPayments);

        http.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()), new Claim("tenantId", tenantId.ToString()), new Claim(ClaimTypes.Role, "TenantOwner")
        ], "test"));
        var ownerResponse = Assert.IsType<TenantDashboardSummary>(Assert.IsType<OkObjectResult>(await controller.GetDashboardSummaryCanonical()).Value);
        Assert.Equal(0m, ownerResponse.MonthlyRevenue);
        Assert.Equal(0, ownerResponse.Alerts.PendingPayments);
        await db.DisposeAsync();
    }

    private sealed class TestTenantState : ICurrentTenantService
    {
        public Guid? TenantId { get; private set; }
        public void SetTenant(Guid tenantId) => TenantId = tenantId;
        public void Clear() => TenantId = null;
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;
        public bool Verify(string password, string passwordHash) => password == passwordHash;
    }
}
