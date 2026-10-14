using System.Text.Json;

namespace CoachPulse.Api.Middleware;

public sealed class LoginRateLimitKeyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering(bufferThreshold: 16 * 1024, bufferLimit: 128 * 1024);
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
                    context.Items["LoginRateLimitEmail"] = email.GetString()?.Trim().ToLowerInvariant() ?? "";
            }
            catch (JsonException) { context.Items["LoginRateLimitEmail"] = "invalid-body"; }
        }
        await next(context);
    }
}
