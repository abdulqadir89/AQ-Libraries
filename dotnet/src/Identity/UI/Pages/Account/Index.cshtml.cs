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
public class AccountIndexModel(
    UserManager<ApplicationUser> userManager,
    IIdentityDbContext context,
    IOpenIddictTokenManager tokenManager) : PageModel
{
    public ApplicationUser CurrentUser { get; set; } = default!;
    public bool TwoFactorEnabled { get; set; }
    public int ActiveSessionCount { get; set; }
    public int ConnectedAppCount { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool HasManageAccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        CurrentUser = user;
        TwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        LastLoginAt = user.LastLoginAt;

        // Count active (non-revoked) tokens for this user
        var tokenCount = 0;
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            var status = await tokenManager.GetStatusAsync(token, HttpContext.RequestAborted);
            if (status == OpenIddictConstants.Statuses.Valid)
                tokenCount++;
        }
        ActiveSessionCount = tokenCount;

        // Connected apps = distinct clients the user has valid tokens for
        var connectedClients = new HashSet<string?>();
        var allTokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in allTokens)
        {
            var status = await tokenManager.GetStatusAsync(token, HttpContext.RequestAborted);
            if (status == OpenIddictConstants.Statuses.Valid)
            {
                var appId = await tokenManager.GetApplicationIdAsync(token, HttpContext.RequestAborted);
                if (appId != null) connectedClients.Add(appId);
            }
        }
        ConnectedAppCount = connectedClients.Count;

        // Check if user has manage_api claim
        HasManageAccess = await context.StoredClaims
            .AsNoTracking()
            .AnyAsync(c => c.UserId == user.Id && c.Type == "manage_api", HttpContext.RequestAborted);

        return Page();
    }
}
