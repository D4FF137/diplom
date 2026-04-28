using Microsoft.AspNetCore.Http;

namespace Shared.Common;

public static class AuthTokenHelper
{
    public const string CookieName = "auth_token";

    public static string? ExtractToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            var headerValue = authorizationHeader.ToString();
            const string bearerPrefix = "Bearer ";

            if (headerValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return headerValue[bearerPrefix.Length..].Trim();
            }
        }

        if (request.Cookies.TryGetValue(CookieName, out var cookieToken) &&
            !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        return null;
    }

    public static string? ExtractToken(HttpContext? httpContext)
    {
        return httpContext == null ? null : ExtractToken(httpContext.Request);
    }
}
