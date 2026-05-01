using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using OpenIddict.Server.AspNetCore;

namespace AQ.Identity.UI.Pages.Auth;

[EnableRateLimiting("auth_endpoints")]
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

    public LoginModel(SignInManager<ApplicationUser> signInManager, IOptions<AqIdentityOptions> options)
    {
        _signInManager = signInManager;
        _options = options;
    }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        ShowGoogleButton = _options.Value.Google != null;
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
            return RedirectToPage("/Auth/Lockout");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("/Auth/Mfa/Challenge", new { returnUrl = ReturnUrl });
        }

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        ModelState.AddModelError(string.Empty, "Incorrect email or password");
        ShowGoogleButton = _options.Value.Google != null;
        return Page();
    }
}
