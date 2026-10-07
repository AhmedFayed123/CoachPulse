using System.Security.Claims;
using CoachPulse.Application.Interfaces;

namespace CoachPulse.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenantService currentTenantService)
    {
        var tenantClaim = context.User.FindFirst("tenantId");

        if (tenantClaim != null &&
            Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            currentTenantService.SetTenant(tenantId);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            currentTenantService.Clear();
        }
    }
}