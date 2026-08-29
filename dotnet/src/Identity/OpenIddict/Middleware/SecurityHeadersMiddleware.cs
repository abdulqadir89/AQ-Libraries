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

        // Strict-Transport-Security is set by ASP.NET Core's built-in HstsMiddleware
        // (see UseAqIdentity/app.UseHsts()), not here — that gives correct preload/
        // max-age/excluded-host semantics instead of a hand-rolled duplicate.

        await _next(context);
    }
}
