using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.UI.Resources;
using QRCoder;
using System.Text;

namespace AQ.Identity.UI.Pages.Mfa;

[Authorize]
public class SetupModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly IStringLocalizer<IdentityUIResource> _localizer;

    [BindProperty]
    public string AuthenticatorKey { get; set; } = default!;

    [BindProperty]
    public string VerificationCode { get; set; } = default!;

    public string QrCodeDataUri { get; set; } = default!;

    public SetupModel(
        UserManager<ApplicationUser> userManager,
        IOptions<AqIdentityOptions> options,
        IStringLocalizer<IdentityUIResource> localizer)
    {
        _userManager = userManager;
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

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        AuthenticatorKey = key ?? string.Empty;
        QrCodeDataUri = GenerateQrCode(user.Email ?? string.Empty, AuthenticatorKey);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(VerificationCode) || !VerificationCode.All(char.IsDigit) || VerificationCode.Length != 6)
        {
            ModelState.AddModelError(string.Empty, _localizer["Invalid code format. Please enter a 6-digit code."]);
            QrCodeDataUri = GenerateQrCode(user.Email ?? string.Empty, AuthenticatorKey);
            return Page();
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, VerificationCode);
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, _localizer["Invalid code. Try again."]);
            QrCodeDataUri = GenerateQrCode(user.Email ?? string.Empty, AuthenticatorKey);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
        TempData["BackupCodes"] = string.Join(",", recoveryCodes ?? []);

        return RedirectToPage("BackupCodes");
    }

    private string GenerateQrCode(string email, string key)
    {
        var otpauthUrl = $"otpauth://totp/{_options.Value.AppName}:{email}?secret={key}&issuer={_options.Value.AppName}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        var qrCodeImage = qrCode.GetGraphic(10);
        return Convert.ToBase64String(qrCodeImage);
    }
}
