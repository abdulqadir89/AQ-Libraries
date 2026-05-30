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
    IOpenIddictApplicationManager applicationManager) : PageModel
{
    public List<SessionRow> Sessions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            var status = await tokenManager.GetStatusAsync(token, HttpContext.RequestAborted);
            if (status != OpenIddictConstants.Statuses.Valid) continue;

            var type = await tokenManager.GetTypeAsync(token, HttpContext.RequestAborted);
            if (type != OpenIddictConstants.TokenTypeHints.AccessToken) continue;

            var tokenId = await tokenManager.GetIdAsync(token, HttpContext.RequestAborted);
            var creationDate = await tokenManager.GetCreationDateAsync(token, HttpContext.RequestAborted);
            var expirationDate = await tokenManager.GetExpirationDateAsync(token, HttpContext.RequestAborted);
            var appId = await tokenManager.GetApplicationIdAsync(token, HttpContext.RequestAborted);

            string? appName = null;
            if (!string.IsNullOrEmpty(appId))
            {
                var app = await applicationManager.FindByIdAsync(appId, HttpContext.RequestAborted);
                if (app != null)
                    appName = await applicationManager.GetDisplayNameAsync(app, HttpContext.RequestAborted);
            }

            Sessions.Add(new SessionRow
            {
                TokenId = tokenId ?? string.Empty,
                AppName = appName ?? "Unknown App",
                CreatedAt = creationDate ?? DateTimeOffset.UtcNow,
                ExpiresAt = expirationDate
            });
        }

        Sessions = [.. Sessions.OrderByDescending(s => s.CreatedAt)];
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(string tokenId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        var token = await tokenManager.FindByIdAsync(tokenId, HttpContext.RequestAborted);
        if (token != null)
        {
            var subject = await tokenManager.GetSubjectAsync(token, HttpContext.RequestAborted);
            if (subject == user.Id.ToString())
                await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);
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

        // Revoke all tokens for this user
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
            await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);

        // Sign out current session and redirect to login
        await signInManager.SignOutAsync();

        return RedirectToPage("/Auth/Login");
    }
}

public class SessionRow
{
    public string TokenId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
