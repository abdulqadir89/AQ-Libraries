using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Utilities.Email;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class SecurityModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;

    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }

    [BindProperty]
    public string RegenerateCodesPassword { get; set; } = string.Empty;

    public string? RegenerateCodesError { get; set; }

    public SecurityModel(
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext context,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        TwoFactorEnabled = user.TwoFactorEnabled;
        if (TwoFactorEnabled)
        {
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRegenerateBackupCodesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        TwoFactorEnabled = user.TwoFactorEnabled;

        if (!TwoFactorEnabled)
        {
            return RedirectToPage();
        }

        if (string.IsNullOrEmpty(RegenerateCodesPassword) || !await _userManager.CheckPasswordAsync(user, RegenerateCodesPassword))
        {
            RegenerateCodesError = "Incorrect password.";
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
            return Page();
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
        TempData["BackupCodes"] = string.Join(",", recoveryCodes ?? []);

        _context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.BackupCodesRegenerated, user.Id, null, null));
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        await SendSecurityAlertAsync(user.Email!, "Your two-factor backup codes were regenerated");

        return RedirectToPage("/Mfa/BackupCodes");
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            TwoFactorEnabled = user.TwoFactorEnabled;
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            TwoFactorEnabled = user.TwoFactorEnabled;
            return Page();
        }

        await SendSecurityAlertAsync(user.Email!, "Your password was changed");

        TempData["AccountSuccess"] = "Your password has been changed successfully.";
        return RedirectToPage();
    }

    private async Task SendSecurityAlertAsync(string toEmail, string eventDescription)
    {
        try
        {
            var message = _emailTemplateService.BuildSecurityAlertEmail(toEmail, eventDescription, _options.Value.AppName);
            await _emailService.SendAsync(message);
        }
        catch
        {
            // Best-effort notification — never block the security-relevant action itself on email delivery.
        }
    }
}
