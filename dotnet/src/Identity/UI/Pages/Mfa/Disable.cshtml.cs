using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.UI.Resources;
using AQ.Utilities.Email;

namespace AQ.Identity.UI.Pages.Mfa;

[Authorize]
public class DisableModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly IStringLocalizer<IdentityUIResource> _localizer;

    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public DisableModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options,
        IStringLocalizer<IdentityUIResource> localizer)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (!user.TwoFactorEnabled)
        {
            return RedirectToPage("/Account/Security");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(CurrentPassword) || !await _userManager.CheckPasswordAsync(user, CurrentPassword))
        {
            ErrorMessage = _localizer["Incorrect password."];
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        try
        {
            var message = _emailTemplateService.BuildSecurityAlertEmail(
                user.Email!, "Two-factor authentication was disabled", _options.Value.AppName);
            await _emailService.SendAsync(message);
        }
        catch
        {
            // Best-effort notification — never block the security-relevant action itself on email delivery.
        }

        TempData["AccountSuccess"] = _localizer["Two-factor authentication was disabled"].Value;
        return RedirectToPage("/Account/Security");
    }
}
