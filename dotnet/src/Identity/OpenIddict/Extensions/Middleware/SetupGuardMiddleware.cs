using AQ.Identity.OpenIddict.Extensions.Seeding;
using Microsoft.AspNetCore.Http;

namespace AQ.Identity.OpenIddict.Extensions.Middleware;

public class SetupGuardMiddleware(RequestDelegate next)
{
    private static readonly string[] _exemptPrefixes =
    [
        "/setup",
        "/_content/",
        "/health",
        "/favicon.ico",
        "/connect/",
        "/auth/",
    ];

    public async Task InvokeAsync(HttpContext context, ISetupStateService setupState)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var isExempt = _exemptPrefixes.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isExempt && await setupState.IsSetupRequiredAsync(context.RequestAborted))
        {
            context.Response.Redirect("/setup");
            return;
        }

        if (path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase)
            && !await setupState.IsSetupRequiredAsync(context.RequestAborted))
        {
            context.Response.Redirect("/manage/clients");
            return;
        }

        await next(context);
    }
}
