using CoachPulse.Application.Interfaces;

namespace CoachPulse.Api.Middleware
{
    public class TenantResolutionMiddleware
    {
        private const string TenantHeader = "X-Tenant-Id";

        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ICurrentTenantService currentTenantService)
        {
            if (context.Request.Headers.TryGetValue(
                TenantHeader,
                out var tenantHeader))
            {
                if (Guid.TryParse(
                    tenantHeader.ToString(),
                    out var tenantId))
                {
                    currentTenantService.SetTenant(tenantId);
                }
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
}
