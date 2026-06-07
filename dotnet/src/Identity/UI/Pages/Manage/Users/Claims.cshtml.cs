using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AQ.Identity.UI.Pages.Manage.Users;

[Authorize(Policy = "ManageApi")]
public class ClaimsModel(
    IIdentityDbContext context,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public ApplicationUser TargetUser { get; set; } = default!;
    public List<UserClaim> Claims { get; set; } = [];

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [TempData] public string? Success { get; set; }
    [TempData] public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.FindByIdAsync(Id.ToString());
        if (user == null) return NotFound();

        TargetUser = user;
        Claims = await context.StoredClaims
            .AsNoTracking()
            .Where(c => c.UserId == Id)
            .OrderBy(c => c.Type)
            .ToListAsync(HttpContext.RequestAborted);

        return Page();
    }

    public async Task<IActionResult> OnPostAddClaimAsync(string claimType, string claimValue)
    {
        claimType = claimType?.Trim() ?? string.Empty;
        claimValue = claimValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(claimType) || string.IsNullOrEmpty(claimValue))
        {
            Error = "Claim type and value are required.";
            return await ReloadAndReturn();
        }

        var user = await userManager.FindByIdAsync(Id.ToString());
        if (user == null) return NotFound();

        var claim = UserClaim.Create(Id, claimType, claimValue);
        context.StoredClaims.Add(claim);
        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.UserClaimAdded, Id, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        Success = $"Claim '{claimType}' added.";
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostDeleteClaimAsync(Guid claimId)
    {
        var claim = await context.StoredClaims
            .FirstOrDefaultAsync(c => c.Id == claimId && c.UserId == Id, HttpContext.RequestAborted);

        if (claim == null) return NotFound();

        // Prevent revoking own manage_api claim
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(currentUserId, out var currentId) &&
            currentId == Id &&
            claim.Type == "manage_api")
        {
            Error = "You cannot remove your own manage_api claim.";
            return await ReloadAndReturn();
        }

        context.StoredClaims.Remove(claim);
        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.UserClaimRemoved, Id, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        Success = $"Claim '{claim.Type}' removed.";
        return RedirectToPage(new { Id });
    }

    private async Task<IActionResult> ReloadAndReturn()
    {
        var user = await userManager.FindByIdAsync(Id.ToString());
        if (user == null) return NotFound();

        TargetUser = user;
        Claims = await context.StoredClaims
            .AsNoTracking()
            .Where(c => c.UserId == Id)
            .OrderBy(c => c.Type)
            .ToListAsync(HttpContext.RequestAborted);

        return Page();
    }
}
