using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly ILogger<RegisterModel> _logger;

    [BindProperty]
    public string FullName { get; set; } = default!;

    [BindProperty]
    public string Email { get; set; } = default!;

    [BindProperty]
    public string Password { get; set; } = default!;

    [BindProperty]
    public string ConfirmPassword { get; set; } = default!;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Email,
            Email = Email,
            FullName = FullName
        };

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

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var verificationUrl = Url.Page(
                "/Auth/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, code = token },
                protocol: Request.Scheme,
                host: Request.Host.ToUriComponent()) ?? string.Empty;

            var emailMessage = _emailTemplateService.BuildVerificationEmail(
                user.Email!,
                verificationUrl,
                _options.Value.AppName);

            await _emailService.SendAsync(emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email for user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while sending the verification email. Please try again.");
            return Page();
        }

        return RedirectToPage("/Auth/VerifyEmailSent", new { email = Email });
    }
}
