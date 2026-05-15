using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

public class ExternalCallbackModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExternalCallbackModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(string returnUrl)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded)
        {
            return RedirectToPage("/Auth/Login", new { error = "external_auth_failed" });
        }

        var externalPrincipal = result.Principal;
        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);
        var name = externalPrincipal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            return RedirectToPage("/Auth/Login", new { error = "no_email" });
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user != null)
        {
            var logins = await _userManager.GetLoginsAsync(user);
            var hasExternalLogin = logins.Any(l => l.LoginProvider == "Google");

            if (!hasExternalLogin)
            {
                var providerKey = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (providerKey == null)
                {
                    return RedirectToPage("/Auth/Login", new { error = "invalid_external_id" });
                }

                var externalLogin = new UserLoginInfo("Google", providerKey, "Google");
                await _userManager.AddLoginAsync(user, externalLogin);
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            else
            {
                var providerKey = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (providerKey == null)
                {
                    return RedirectToPage("/Auth/Login", new { error = "invalid_external_id" });
                }

                await _signInManager.ExternalLoginSignInAsync("Google", providerKey, isPersistent: false);
            }
        }
        else
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = name ?? string.Empty
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return RedirectToPage("/Auth/Login", new { error = "user_creation_failed" });
            }

            var providerKey = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (providerKey == null)
            {
                return RedirectToPage("/Auth/Login", new { error = "invalid_external_id" });
            }

            var externalLogin = new UserLoginInfo("Google", providerKey, "Google");
            await _userManager.AddLoginAsync(user, externalLogin);
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}