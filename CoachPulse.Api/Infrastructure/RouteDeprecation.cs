namespace CoachPulse.Api.Infrastructure;

internal sealed class RouteDeprecationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        string? successor = null;

        if (method == "POST" && path == "/api/auth/accept-invitation") successor = "/api/auth/register-client";
        else if (method == "POST" && path == "/api/coach/clients/invite") successor = "/api/clients/invite";
        else if (method == "GET" && path == "/api/coach/clients") successor = "/api/clients";
        else if (path == "/api/coach/programs" && method == "GET") successor = "/api/programs";
        else if (path == "/api/coach/programs" && method == "POST") successor = "/api/programs";
        else if (path == "/api/coach/exercises" && method == "GET") successor = "/api/exercises";
        else if (path == "/api/coach/exercises" && method == "POST") successor = "/api/exercises";
        else if (method == "GET" && path == "/api/coach/programs/templates") successor = "/api/programs/templates";
        else if (method == "POST" && path == "/api/coach/programs/templates") successor = "/api/programs/{id}/save-as-template";
        else if (method == "POST" && path.StartsWith("/api/coach/programs/templates/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/create", StringComparison.OrdinalIgnoreCase))
        {
            var templateId = path["/api/coach/programs/templates/".Length..^"/create".Length];
            successor = $"/api/programs/{templateId}/from-template";
        }
        else if (path.StartsWith("/api/coach/programs/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/exercises", StringComparison.OrdinalIgnoreCase))
        {
            var programId = path["/api/coach/programs/".Length..^"/exercises".Length];
            successor = $"/api/programs/{programId}/exercises";
        }
        else if (method == "GET" && path == "/api/tenant/dashboard-summary") successor = "/api/tenant/dashboard/summary";
        else if (method == "GET" && path == "/api/tenant/churn-risk") successor = "/api/tenant/dashboard/churn-risk";
        else if (method == "GET" && path.StartsWith("/api/chat/conversations/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
        {
            var clientId = path["/api/chat/conversations/".Length..^"/messages".Length];
            successor = $"/api/messages/{clientId}";
        }

        if (successor is not null)
        {
            context.Response.Headers["Deprecation"] = "true";
            context.Response.Headers["Link"] = $"<{successor}>; rel=\"successor-version\"";
        }

        await next(context);
    }
}

internal static class RouteDeprecation
{
    public static void AddHeaders(HttpResponse response, string successor)
    {
        response.Headers["Deprecation"] = "true";
        response.Headers["Link"] = $"<{successor}>; rel=\"successor-version\"";
    }
}
