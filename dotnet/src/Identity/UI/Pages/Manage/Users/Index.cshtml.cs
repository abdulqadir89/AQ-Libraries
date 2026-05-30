using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Manage.Users;

[Authorize(Policy = "ManageApi")]
public class UsersIndexModel(
    IIdentityDbContext context,
    UserManager<ApplicationUser> userManager,
    IOpenIddictTokenManager tokenManager) : PageModel
{
    public List<UserRow> Users { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await userManager.UpdateAsync(user);

        context.AuditLog.Add(AuditEntry.Log(
            user.IsActive ? AuditEntry.Actions.UserActivated : AuditEntry.Actions.UserDeactivated,
            userId, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"User '{user.Email}' has been {(user.IsActive ? "activated" : "deactivated")}.";
        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostInvalidateSessionsAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        // Rotate the security stamp so all existing tokens are rejected
        await userManager.UpdateSecurityStampAsync(user);

        // Revoke all OpenIddict tokens for this subject
        var tokens = tokenManager.FindBySubjectAsync(userId.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
            await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.UserSessionsRevoked, userId, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"All sessions for '{user.Email}' have been invalidated.";
        return RedirectToPage(new { Search });
    }

    private async Task LoadUsersAsync()
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var lower = Search.ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(lower) ||
                u.FullName.ToLower().Contains(lower));
        }

        Users = await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserRow
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(HttpContext.RequestAborted);
    }
}

public class UserRow
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
