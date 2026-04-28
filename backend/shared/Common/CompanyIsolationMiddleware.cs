using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Common;

public class CompanyIsolationMiddleware
{
    private readonly RequestDelegate _next;

    public CompanyIsolationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var companyIdClaim = context.User.FindFirst("companyId")?.Value;
            if (!string.IsNullOrEmpty(companyIdClaim))
            {
                context.Items["CompanyId"] = companyIdClaim;
            }
        }

        await _next(context);
    }
}


