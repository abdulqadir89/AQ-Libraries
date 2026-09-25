using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.UI.Resources;
using OpenIddict.Server.AspNetCore;

namespace AQ.Identity.UI.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly IStringLocalizer<IdentityUIResource> _localizer;

    [BindProperty]
    public string Email { get; set; } = default!;

    [BindProperty]
    public string Password { get; set; } = default!;

    [BindProperty]
    public bool RememberMe { get; set; }

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public bool ShowGoogleButton { get; set; }

    public string? ExternalError { get; set; }

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        IOptions<AqIdentityOptions> options,
        IStringLocalizer<IdentityUIResource> localizer)
    {
        _signInManager = signInManager;
        _options = options;
        _localizer = localizer;
    }

    public void OnGet(string? returnUrl, string? error)
    {
        ReturnUrl = returnUrl;
        ShowGoogleButton = _options.Value.Google != null;
        ExternalError = error switch
        {
            "email_not_verified" => _localizer["That Google account's email isn't verified. Please verify it with Google, or sign in with your password instead."].Value,
            "no_email" => _localizer["Your Google account doesn't have an email address we can use."].Value,
            "external_auth_failed" => _localizer["Google sign-in failed. Please try again."].Value,
            "user_creation_failed" or "invalid_external_id" => _localizer["Something went wrong signing in with Google. Please try again."].Value,
            _ => null,
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ShowGoogleButton = _options.Value.Google != null;
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Email, Password, RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(Email);
            var lockoutEnd = user != null ? await _signInManager.UserManager.GetLockoutEndDateAsync(user) : null;
            return RedirectToPage("/Auth/Lockout", new { until = lockoutEnd?.UtcTicks });
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("/Mfa/Challenge", new { returnUrl = ReturnUrl });
        }

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Apps/Index");
        }

        ModelState.AddModelError(string.Empty, _localizer["Incorrect email or password"]);
        ShowGoogleButton = _options.Value.Google != null;
        return Page();
    }
}
