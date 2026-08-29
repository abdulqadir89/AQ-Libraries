using Microsoft.AspNetCore.Http;

namespace AQ.Identity.OpenIddict.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // script-src and style-src still need 'unsafe-inline' — the Razor Pages UI uses
        // inline <script> and <style> blocks throughout every shared layout (login
        // validation, modals, MFA setup, theming, etc). Migrating to nonces/external
        // files would meaningfully improve XSS defense-in-depth but touches every page
        // in the UI project — a larger, separate change from this fix pass.
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'";

        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        await _next(context);
    }
}
