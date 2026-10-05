using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace AQ.Identity.OpenIddict.Sessions;

/// <summary>
/// Gives each IdP sign-in a browser session id (OIDC "sid"), kept in the Identity cookie.
/// The authorize endpoint copies it into the tokens and ClaimsEnrichmentHandler stores it on
/// the app session it creates, so one sign-out can end every app signed in from this browser.
/// </summary>
public static class BrowserSession
{
    public const string SessionIdClaim = "sid";

    /// <summary>
    /// Cookie OnSigningIn hook: adds a new sid on a fresh sign-in, and keeps the current one
    /// when the same user's cookie is reissued (RefreshSignInAsync after a profile or password
    /// change), so apps already signed in stay tied to this browser session.
    /// </summary>
    public static async Task OnSigningInAsync(CookieSigningInContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity) return;
        if (identity.HasClaim(c => c.Type == SessionIdClaim)) return;

        var sessionId = Guid.NewGuid().ToString("N");

        var current = await context.HttpContext.AuthenticateAsync(context.Scheme.Name);
        var currentSessionId = current.Principal?.FindFirst(SessionIdClaim)?.Value;
        var sameUser = current.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            == identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (current.Succeeded && sameUser && !string.IsNullOrEmpty(currentSessionId))
        {
            sessionId = currentSessionId;
        }

        identity.AddClaim(new Claim(SessionIdClaim, sessionId));
    }

    /// <summary>
    /// SecurityStampValidator rebuilds the cookie principal from the user record on its
    /// periodic check, which would drop the sid; carry it over.
    /// </summary>
    public static Task OnRefreshingPrincipal(SecurityStampRefreshingPrincipalContext context)
    {
        var sessionId = context.CurrentPrincipal?.FindFirst(SessionIdClaim)?.Value;
        if (!string.IsNullOrEmpty(sessionId)
            && context.NewPrincipal?.Identity is ClaimsIdentity identity
            && !identity.HasClaim(c => c.Type == SessionIdClaim))
        {
            identity.AddClaim(new Claim(SessionIdClaim, sessionId));
        }

        return Task.CompletedTask;
    }
}
