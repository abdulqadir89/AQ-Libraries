using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

[EnableRateLimiting("auth_endpoints")]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly ILogger<ForgotPasswordModel> _logger;

    [BindProperty]
    public string Email { get; set; } = default!;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options,
        ILogger<ForgotPasswordModel> logger)
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

        var user = await _userManager.FindByEmailAsync(Email);

        if (user != null && await _userManager.IsEmailConfirmedAsync(user))
        {
            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = Url.Page(
                    "/Auth/ResetPassword",
                    pageHandler: null,
                    values: new { userId = user.Id, code = token },
                    protocol: Request.Scheme,
                    host: Request.Host.ToUriComponent()) ?? string.Empty;

                var emailMessage = _emailTemplateService.BuildPasswordResetEmail(
                    user.Email!,
                    resetUrl,
                    _options.Value.AppName);

                await _emailService.SendAsync(emailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email for {Email}", Email);
            }
        }

        return RedirectToPage("/Auth/ForgotPasswordConfirmation");
    }
}
