using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AQ.Identity.OpenIddict.Management.Endpoints.Users;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class DeleteAccountModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IIdentityDbContext context,
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IEnumerable<IUserDataLifecycleHook> lifecycleHooks) : PageModel
{
    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    public string? TwoFactorCode { get; set; }

    public bool TwoFactorEnabled { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        TwoFactorEnabled = user.TwoFactorEnabled;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        TwoFactorEnabled = user.TwoFactorEnabled;

        if (string.IsNullOrEmpty(CurrentPassword) || !await userManager.CheckPasswordAsync(user, CurrentPassword))
        {
            ErrorMessage = "Incorrect password.";
            return Page();
        }

        if (TwoFactorEnabled)
        {
            var validTotp = !string.IsNullOrEmpty(TwoFactorCode) &&
                await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, TwoFactorCode);

            var validRecoveryCode = !validTotp && !string.IsNullOrEmpty(TwoFactorCode) &&
                (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, TwoFactorCode)).Succeeded;

            if (!validTotp && !validRecoveryCode)
            {
                ErrorMessage = "Invalid two-factor code.";
                return Page();
            }
        }

        var holdsAdminClaim = await context.StoredClaims
            .AnyAsync(c => c.UserId == user.Id && c.Type == AdminClaimGuard.ManageApiClaimType, HttpContext.RequestAborted);

        if (holdsAdminClaim && await AdminClaimGuard.WouldRemoveLastAdminAsync(context, user.Id, HttpContext.RequestAborted))
        {
            ErrorMessage = "You're the last administrator — transfer admin access to another account before deleting yours.";
            return Page();
        }

        foreach (var hook in lifecycleHooks)
        {
            await hook.OnBeforeUserDeletedAsync(user.Id, HttpContext.RequestAborted);
        }

        await authorizationManager.RevokeBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            await tokenManager.TryRevokeAsync(token, HttpContext.RequestAborted);
        }

        var claims = context.StoredClaims.Where(c => c.UserId == user.Id);
        context.StoredClaims.RemoveRange(claims);

        // Logged before the user row is deleted — AuditEntry.UserId is nullable specifically
        // so this entry (and any earlier ones for this user) survive the delete without an
        // FK violation.
        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.AccountDeleted, user.Id, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        await signInManager.SignOutAsync();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            ErrorMessage = "Something went wrong deleting your account. Please try again or contact support.";
            return Page();
        }

        return RedirectToPage("/Auth/Login");
    }
}
