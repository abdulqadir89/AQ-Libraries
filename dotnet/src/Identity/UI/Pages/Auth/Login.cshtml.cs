using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using OpenIddict.Server.AspNetCore;

namespace AQ.Identity.UI.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOptions<AqIdentityOptions> _options;

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

    public LoginModel(SignInManager<ApplicationUser> signInManager, IOptions<AqIdentityOptions> options)
    {
        _signInManager = signInManager;
        _options = options;
    }

    public void OnGet(string? returnUrl, string? error)
    {
        ReturnUrl = returnUrl;
        ShowGoogleButton = _options.Value.Google != null;
        ExternalError = error switch
        {
            "email_not_verified" => "That Google account's email isn't verified. Please verify it with Google, or sign in with your password instead.",
            "no_email" => "Your Google account doesn't have an email address we can use.",
            "external_auth_failed" => "Google sign-in failed. Please try again.",
            "user_creation_failed" or "invalid_external_id" => "Something went wrong signing in with Google. Please try again.",
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

        ModelState.AddModelError(string.Empty, "Incorrect email or password");
        ShowGoogleButton = _options.Value.Google != null;
        return Page();
    }
}
