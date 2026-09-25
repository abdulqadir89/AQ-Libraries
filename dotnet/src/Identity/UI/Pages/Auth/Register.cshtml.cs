using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.UI.Resources;
using AQ.Utilities.Email;

namespace AQ.Identity.UI.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IStringLocalizer<IdentityUIResource> _localizer;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string FullName { get; set; } = default!;

    [BindProperty]
    public string Email { get; set; } = default!;

    [BindProperty]
    public string Password { get; set; } = default!;

    [BindProperty]
    public string ConfirmPassword { get; set; } = default!;

    public int PasswordMinLength => _options.Value.Password.MinLength;
    public bool PasswordRequireDigit => _options.Value.Password.RequireDigit;
    public bool PasswordRequireUppercase => _options.Value.Password.RequireUppercase;
    public bool PasswordRequireNonAlphanumeric => _options.Value.Password.RequireNonAlphanumeric;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options,
        ILogger<RegisterModel> logger,
        IStringLocalizer<IdentityUIResource> localizer)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options;
        _logger = logger;
        _localizer = localizer;
    }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", _localizer["Passwords do not match"]);
            return Page();
        }

        var user = ApplicationUser.Create(Email, FullName);

        var result = await _userManager.CreateAsync(user, Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var fieldName = error.Code switch
                {
                    "DuplicateUserName" or "DuplicateEmail" => "Email",
                    "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit" or "PasswordRequiresUpper" or "PasswordRequiresLower" => "Password",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(fieldName))
                {
                    ModelState.AddModelError(fieldName, error.Description);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        // error.Description here already comes localized: RegisterModel resolves
        // UserManager<ApplicationUser> from DI, whose IdentityErrorDescriber is
        // LocalizedIdentityErrorDescriber (registered in AddAqIdentityLocalization).

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var issuer = _options.Value.Issuer ?? "http://localhost:5001";
            var verificationUrl = $"{issuer}/auth/verify-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(token)}";

            var emailMessage = _emailTemplateService.BuildVerificationEmail(
                user.Email!,
                verificationUrl,
                _options.Value.AppName);

            await _emailService.SendAsync(emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email for user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, _localizer["An error occurred while sending the verification email. Please try again."]);
            return Page();
        }

        return RedirectToPage("/Auth/VerifyEmailSent", new { email = Email, returnUrl = ReturnUrl });
    }
}
