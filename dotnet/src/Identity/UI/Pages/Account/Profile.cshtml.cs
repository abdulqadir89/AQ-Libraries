using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Utilities.Email;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly ILogger<ProfileModel> _logger;

    [BindProperty]
    public string Email { get; set; } = default!;

    [BindProperty]
    public string FullName { get; set; } = default!;

    [BindProperty]
    public string NewEmail { get; set; } = default!;

    public ProfileModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options,
        ILogger<ProfileModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        Email = user.Email!;
        FullName = user.FullName;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            Email = user.Email!;
            return Page();
        }

        user.FullName = FullName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            Email = user.Email!;
            return Page();
        }

        TempData["AccountSuccess"] = "Your profile has been updated successfully.";
        return RedirectToPage();
    }

    // Never changes user.Email directly — always requires confirmation on the new address
    // first (mirrors ASP.NET Core Identity's own change-email token flow), so an attacker
    // with a hijacked session can't silently redirect account-recovery emails to an address
    // they control.
    public async Task<IActionResult> OnPostRequestEmailChangeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        Email = user.Email!;
        FullName = user.FullName;

        if (string.IsNullOrWhiteSpace(NewEmail) || !new EmailAddressAttribute().IsValid(NewEmail))
        {
            ModelState.AddModelError("NewEmail", "Enter a valid email address.");
            return Page();
        }

        if (string.Equals(NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("NewEmail", "That's already your current email address.");
            return Page();
        }

        var existing = await _userManager.FindByEmailAsync(NewEmail);
        if (existing != null)
        {
            ModelState.AddModelError("NewEmail", "That email address is already in use.");
            return Page();
        }

        try
        {
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, NewEmail);
            var issuer = _options.Value.Issuer ?? "http://localhost:5001";
            var confirmUrl = $"{issuer}/auth/confirm-email-change" +
                $"?userId={Uri.EscapeDataString(user.Id.ToString())}" +
                $"&newEmail={Uri.EscapeDataString(NewEmail)}" +
                $"&code={Uri.EscapeDataString(token)}";

            var message = _emailTemplateService.BuildVerificationEmail(NewEmail, confirmUrl, _options.Value.AppName);
            await _emailService.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email-change confirmation for user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Something went wrong sending the confirmation email. Please try again.");
            return Page();
        }

        TempData["AccountSuccess"] = $"We've sent a confirmation link to {NewEmail}. Your email won't change until you confirm it.";
        return RedirectToPage();
    }
}
