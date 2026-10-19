namespace CoachPulse.Api.Middleware;

public sealed class FirstLoginPasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var required = context.User.Identity?.IsAuthenticated == true &&
                       context.User.HasClaim("mustChangePassword", "true");
        var allowed = context.Request.Path.Equals("/api/account/change-password", StringComparison.OrdinalIgnoreCase) ||
                      context.Request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase) ||
                      context.Request.Path.Equals("/api/auth/logout-all", StringComparison.OrdinalIgnoreCase);

        if (required && !allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/403",
                title = "Password change required",
                status = StatusCodes.Status403Forbidden,
                detail = "Change the initial password before using the application."
            });
            return;
        }

        await next(context);
    }
}
