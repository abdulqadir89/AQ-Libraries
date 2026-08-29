using System.Text.Json;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class ExportDataModel(
    UserManager<ApplicationUser> userManager,
    IIdentityDbContext context,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager,
    IEnumerable<IUserDataLifecycleHook> lifecycleHooks) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var claims = await context.StoredClaims
            .AsNoTracking()
            .Where(c => c.UserId == user.Id)
            .Select(c => new { c.Type, c.Value })
            .ToListAsync(HttpContext.RequestAborted);

        var connectedApps = new List<object>();
        var authorizations = authorizationManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var authorization in authorizations)
        {
            if (await authorizationManager.GetStatusAsync(authorization, HttpContext.RequestAborted) != OpenIddictConstants.Statuses.Valid)
                continue;

            var appId = await authorizationManager.GetApplicationIdAsync(authorization, HttpContext.RequestAborted);
            var appName = appId != null
                ? await applicationManager.GetDisplayNameAsync(
                    (await applicationManager.FindByIdAsync(appId, HttpContext.RequestAborted))!, HttpContext.RequestAborted)
                : null;

            connectedApps.Add(new
            {
                Application = appName ?? "Unknown",
                CreatedAt = await authorizationManager.GetCreationDateAsync(authorization, HttpContext.RequestAborted),
                Scopes = await authorizationManager.GetScopesAsync(authorization, HttpContext.RequestAborted),
            });
        }

        var appData = new Dictionary<string, object?>();
        foreach (var hook in lifecycleHooks)
        {
            foreach (var (key, value) in await hook.ExportUserDataAsync(user.Id, HttpContext.RequestAborted))
            {
                appData[key] = value;
            }
        }

        var export = new
        {
            ExportedAt = DateTimeOffset.UtcNow,
            Account = new
            {
                user.Id,
                user.Email,
                user.EmailConfirmed,
                user.FullName,
                user.CreatedAt,
                user.LastLoginAt,
                user.TwoFactorEnabled,
            },
            Claims = claims,
            ConnectedApplications = connectedApps,
            ApplicationData = appData,
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        return File(bytes, "application/json", $"account-data-export-{DateTimeOffset.UtcNow:yyyyMMdd}.json");
    }
}
