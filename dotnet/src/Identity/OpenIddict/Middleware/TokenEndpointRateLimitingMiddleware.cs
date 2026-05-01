using Microsoft.AspNetCore.Http;

namespace AQ.Identity.OpenIddict.Middleware;

/// <summary>
/// Rate limiting for the /connect/token endpoint. Marks requests to apply the token_endpoint policy.
/// Note: The rate limiting is applied by the rate limiter middleware configured in AddAqIdentity.
/// </summary>
public class TokenEndpointRateLimitingMiddleware
{
    private readonly RequestDelegate _next;

    public TokenEndpointRateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Mark token endpoint requests for rate limiting
        if (context.Request.Path.StartsWithSegments("/connect/token"))
        {
            context.Items["endpoint"] = "token";
        }

        await _next(context);
    }
}
