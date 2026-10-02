using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

public class ExternalCallbackModel : PageModel
{
    private const string GoogleProvider = "Google";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExternalCallbackModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded)
        {
            return RedirectToPage("/Auth/Login", new { error = "external_auth_failed" });
        }

        var externalPrincipal = result.Principal;
        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);
        var name = externalPrincipal.FindFirstValue(ClaimTypes.Name);
        var providerKey = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailVerified = externalPrincipal.FindFirstValue("email_verified");
        var isEmailVerified = string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(email))
        {
            return RedirectToPage("/Auth/Login", new { error = "no_email" });
        }

        if (string.IsNullOrEmpty(providerKey))
        {
            return RedirectToPage("/Auth/Login", new { error = "invalid_external_id" });
        }

        // Prefer the stored provider link over email matching: a user may have changed
        // their email since linking, and the link is what proves account ownership.
        var user = await _userManager.FindByLoginAsync(GoogleProvider, providerKey)
                   ?? await _userManager.FindByEmailAsync(email);

        if (user != null)
        {
            if (!user.IsActive)
            {
                return RedirectToPage("/Auth/Login", new { error = "account_disabled" });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                return RedirectToPage("/Auth/Lockout", new { until = lockoutEnd?.UtcTicks });
            }

            var logins = await _userManager.GetLoginsAsync(user);
            var isLinked = logins.Any(l => l.LoginProvider == GoogleProvider && l.ProviderKey == providerKey);

            if (!isLinked)
            {
                // Only auto-link to an existing password account if the external
                // provider has itself verified the email — otherwise anyone who
                // controls an unverified address could take over the local account.
                if (!isEmailVerified)
                {
                    return RedirectToPage("/Auth/Login", new { error = "email_not_verified" });
                }

                var addResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(GoogleProvider, providerKey, GoogleProvider));
                if (!addResult.Succeeded)
                {
                    return RedirectToPage("/Auth/Login", new { error = "external_auth_failed" });
                }

                // The provider vouched for the address, so a still-unconfirmed password
                // account is now confirmed.
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }
            }
        }
        else
        {
            user = ApplicationUser.Create(email, name ?? string.Empty);
            user.EmailConfirmed = isEmailVerified;

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return RedirectToPage("/Auth/Login", new { error = "user_creation_failed" });
            }

            var addResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(GoogleProvider, providerKey, GoogleProvider));
            if (!addResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return RedirectToPage("/Auth/Login", new { error = "user_creation_failed" });
            }
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        await _signInManager.SignInAsync(user, isPersistent: false);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Apps/Index");
    }
}
