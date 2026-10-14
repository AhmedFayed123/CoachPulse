using Hangfire.Dashboard;

namespace CoachPulse.Api.Authorization;

public sealed class SuperAdminHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated == true && user.IsInRole("SuperAdmin"))
            return true;

        httpContext.Response.StatusCode = user.Identity?.IsAuthenticated == true
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        return false;
    }
}
