using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using AQ.Identity.Core.Entities;
using AQ.Identity.UI.Resources;

namespace AQ.Identity.UI.Pages.Mfa;

public class ChallengeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IStringLocalizer<IdentityUIResource> _localizer;
    private const int MaxFailedAttempts = 5;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public ChallengeModel(SignInManager<ApplicationUser> signInManager, IStringLocalizer<IdentityUIResource> localizer)
    {
        _signInManager = signInManager;
        _localizer = localizer;
    }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAuthenticatorAsync(string code)
    {
        if (string.IsNullOrEmpty(code) || !code.All(char.IsDigit) || code.Length != 6)
        {
            ModelState.AddModelError(string.Empty, _localizer["Invalid code format. Please enter a 6-digit code."]);
            return Page();
        }

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, rememberClient: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("/Auth/Lockout");
        }

        ModelState.AddModelError(string.Empty, _localizer["Invalid code. Try again."]);
        return Page();
    }

    public async Task<IActionResult> OnPostBackupCodeAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            ModelState.AddModelError(string.Empty, _localizer["Please enter a backup code."]);
            return Page();
        }

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("/Auth/Lockout");
        }

        ModelState.AddModelError(string.Empty, _localizer["Invalid backup code. Try again."]);
        return Page();
    }
}
