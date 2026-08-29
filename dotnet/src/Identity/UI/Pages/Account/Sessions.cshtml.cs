using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class SessionsModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager) : PageModel
{
    public List<SessionRow> Sessions { get; set; } = [];

    // A "session" is one permanent OpenIddictAuthorization — created once at login by
    // ClaimsEnrichmentHandler and reused across every refresh-token grant for that
    // login. Listing by authorization (not by token) is what keeps this page from
    // showing a fresh row every time the access token silently reissues on an
    // hourly cycle; it reflects actual sign-ins/devices instead.
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        var authorizations = authorizationManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var authorization in authorizations)
        {
            var status = await authorizationManager.GetStatusAsync(authorization, HttpContext.RequestAborted);
            if (status != OpenIddictConstants.Statuses.Valid) continue;

            var authorizationId = await authorizationManager.GetIdAsync(authorization, HttpContext.RequestAborted);
            var creationDate = await authorizationManager.GetCreationDateAsync(authorization, HttpContext.RequestAborted);
            var appId = await authorizationManager.GetApplicationIdAsync(authorization, HttpContext.RequestAborted);

            // Latest expiry among this session's still-valid tokens — reflects how
            // long the session keeps renewing itself via refresh, not any single
            // token's short-lived expiry.
            DateTimeOffset? expiresAt = null;
            var hasValidToken = false;
            var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
            await foreach (var token in tokens)
            {
                var tokenAuthId = await tokenManager.GetAuthorizationIdAsync(token, HttpContext.RequestAborted);
                if (tokenAuthId != authorizationId) continue;

                var tokenStatus = await tokenManager.GetStatusAsync(token, HttpContext.RequestAborted);
                if (tokenStatus != OpenIddictConstants.Statuses.Valid) continue;

                hasValidToken = true;
                var tokenExpiry = await tokenManager.GetExpirationDateAsync(token, HttpContext.RequestAborted);
                if (tokenExpiry.HasValue && (expiresAt is null || tokenExpiry > expiresAt))
                    expiresAt = tokenExpiry;
            }

            // No live token left in this authorization (all expired/redeemed with
            // nothing rotated in yet) — nothing left to revoke, so skip it rather
            // than showing a dead session.
            if (!hasValidToken) continue;

            string? appName = null;
            if (!string.IsNullOrEmpty(appId))
            {
                var app = await applicationManager.FindByIdAsync(appId, HttpContext.RequestAborted);
                if (app != null)
                    appName = await applicationManager.GetDisplayNameAsync(app, HttpContext.RequestAborted);
            }

            Sessions.Add(new SessionRow
            {
                AuthorizationId = authorizationId ?? string.Empty,
                AppName = appName ?? "Unknown App",
                CreatedAt = creationDate ?? DateTimeOffset.UtcNow,
                ExpiresAt = expiresAt
            });
        }

        Sessions = [.. Sessions.OrderByDescending(s => s.CreatedAt)];
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(string authorizationId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        var authorization = await authorizationManager.FindByIdAsync(authorizationId, HttpContext.RequestAborted);
        if (authorization != null)
        {
            var subject = await authorizationManager.GetSubjectAsync(authorization, HttpContext.RequestAborted);
            if (subject == user.Id.ToString())
                await RevokeAuthorizationAndTokensAsync(authorizationId, user.Id.ToString());
        }

        TempData["AccountSuccess"] = "Session has been revoked.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAllAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        // Rotate security stamp so all existing tokens fail validation
        await userManager.UpdateSecurityStampAsync(user);

        await authorizationManager.RevokeBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);

        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
            await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);

        // Sign out current session and redirect to login
        await signInManager.SignOutAsync();

        return RedirectToPage("/Auth/Login");
    }

    private async Task RevokeAuthorizationAndTokensAsync(string authorizationId, string subject)
    {
        var authorization = await authorizationManager.FindByIdAsync(authorizationId, HttpContext.RequestAborted);
        if (authorization != null)
            await authorizationManager.TryRevokeAsync(authorization, HttpContext.RequestAborted);

        // Belt-and-braces: also revoke every token linked to this authorization
        // directly, rather than relying solely on the authorization's revoked status
        // being honored at validation time.
        var tokens = tokenManager.FindBySubjectAsync(subject, HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            var tokenAuthId = await tokenManager.GetAuthorizationIdAsync(token, HttpContext.RequestAborted);
            if (tokenAuthId == authorizationId)
                await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);
        }
    }
}

public class SessionRow
{
    public string AuthorizationId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
