using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class AccountAppsModel(
    UserManager<ApplicationUser> userManager,
    IOpenIddictTokenManager tokenManager,
    IOpenIddictApplicationManager applicationManager) : PageModel
{
    public List<ConnectedApp> Apps { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        // Build a map of appId → most recent token date + scopes
        var appMap = new Dictionary<string, ConnectedApp>();

        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            var status = await tokenManager.GetStatusAsync(token, HttpContext.RequestAborted);
            if (status != OpenIddictConstants.Statuses.Valid) continue;

            var appId = await tokenManager.GetApplicationIdAsync(token, HttpContext.RequestAborted);
            if (string.IsNullOrEmpty(appId)) continue;

            var createdAt = await tokenManager.GetCreationDateAsync(token, HttpContext.RequestAborted);

            if (!appMap.TryGetValue(appId, out var entry))
            {
                var app = await applicationManager.FindByIdAsync(appId, HttpContext.RequestAborted);
                if (app == null) continue;

                var clientId = await applicationManager.GetClientIdAsync(app, HttpContext.RequestAborted) ?? appId;
                var displayName = await applicationManager.GetDisplayNameAsync(app, HttpContext.RequestAborted) ?? clientId;

                entry = new ConnectedApp
                {
                    AppId = appId,
                    ClientId = clientId,
                    DisplayName = displayName,
                    LastUsed = createdAt ?? DateTimeOffset.UtcNow
                };
                appMap[appId] = entry;
            }
            else if (createdAt.HasValue && createdAt.Value > entry.LastUsed)
            {
                entry.LastUsed = createdAt.Value;
            }
        }

        Apps = [.. appMap.Values.OrderByDescending(a => a.LastUsed)];
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(string appId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            var tokenAppId = await tokenManager.GetApplicationIdAsync(token, HttpContext.RequestAborted);
            if (tokenAppId == appId)
                await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);
        }

        TempData["AccountSuccess"] = "Access for that app has been revoked.";
        return RedirectToPage();
    }
}

public class ConnectedApp
{
    public string AppId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset LastUsed { get; set; }
}
