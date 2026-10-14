using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class PlatformRoutePolicyTests
{
    [Fact]
    public void EveryControllerRouteHasExplicitAuthorizationOrAnonymousMetadata()
    {
        var assembly = typeof(CoachPulse.Api.Program).Assembly;
        var missing = assembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().Any(a => a is HttpMethodAttribute))
                .Where(m => m.GetCustomAttribute<AuthorizeAttribute>(true) is null
                    && t.GetCustomAttribute<AuthorizeAttribute>(true) is null
                    && m.GetCustomAttribute<AllowAnonymousAttribute>(true) is null
                    && t.GetCustomAttribute<AllowAnonymousAttribute>(true) is null)
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToArray();

        Assert.Empty(missing);
        Assert.NotNull(typeof(CoachPulse.Api.Hubs.ChatHub).GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(CoachPulse.Api.Hubs.NotificationsHub).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void MinimalApiAndHubMappingsDeclareAuthorizationMetadata()
    {
        var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../CoachPulse.Api/Program.cs"));
        var program = File.ReadAllText(source);
        Assert.Contains("app.MapGet(\"/healthz\"", program);
        Assert.Contains("app.MapGet(\"/readyz\"", program);
        Assert.Contains(".AllowAnonymous()", program);
        Assert.Contains("app.MapHub<ChatHub>(\"/hubs/chat\").RequireAuthorization()", program);
        Assert.Contains("app.MapHub<NotificationsHub>(\"/hubs/notifications\").RequireAuthorization()", program);
    }

    [Theory]
    [InlineData("coachpulse", true)]
    [InlineData("a-2z", true)]
    [InlineData("api", false)]
    [InlineData("hangfire", false)]
    [InlineData("UPPER", false)]
    [InlineData("ab", false)]
    public void PublicSlugValidationRejectsReservedAndMalformedValues(string slug, bool expected)
    {
        Assert.Equal(expected, CoachPulse.Api.Controllers.PublicController.IsValidSlug(slug));
    }

    [Theory]
    [InlineData("POST", "/api/auth/accept-invitation", "/api/auth/register-client")]
    [InlineData("POST", "/api/coach/clients/invite", "/api/clients/invite")]
    [InlineData("GET", "/api/coach/clients", "/api/clients")]
    [InlineData("GET", "/api/coach/programs", "/api/programs")]
    [InlineData("POST", "/api/coach/programs", "/api/programs")]
    [InlineData("GET", "/api/coach/exercises", "/api/exercises")]
    [InlineData("POST", "/api/coach/exercises", "/api/exercises")]
    [InlineData("POST", "/api/coach/programs/plan-id/exercises", "/api/programs/plan-id/exercises")]
    [InlineData("GET", "/api/coach/programs/templates", "/api/programs/templates")]
    [InlineData("POST", "/api/coach/programs/templates", "/api/programs/{id}/save-as-template")]
    [InlineData("POST", "/api/coach/programs/templates/template-id/create", "/api/programs/template-id/from-template")]
    [InlineData("GET", "/api/tenant/dashboard-summary", "/api/tenant/dashboard/summary")]
    [InlineData("GET", "/api/tenant/churn-risk", "/api/tenant/dashboard/churn-risk")]
    [InlineData("GET", "/api/chat/conversations/client-id/messages", "/api/messages/client-id")]
    public async Task LegacyRouteMiddlewareMarksSuccessor(string method, string path, string successor)
    {
        var type = typeof(CoachPulse.Api.Program).Assembly.GetType("CoachPulse.Api.Infrastructure.RouteDeprecationMiddleware", throwOnError: true)!;
        var context = new DefaultHttpContext(); context.Request.Method = method; context.Request.Path = path;
        var middleware = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { new RequestDelegate(_ => Task.CompletedTask) }, null)!;
        var invoke = type.GetMethod("InvokeAsync", BindingFlags.Instance | BindingFlags.Public)!;
        await (Task)invoke.Invoke(middleware, new object[] { context })!;
        Assert.Equal("true", context.Response.Headers["Deprecation"]);
        Assert.Equal($"<{successor}>; rel=\"successor-version\"", context.Response.Headers["Link"]);
    }
}
