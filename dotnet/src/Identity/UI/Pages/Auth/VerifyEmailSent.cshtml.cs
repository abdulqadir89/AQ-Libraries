using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

public class VerifyEmailSentModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AqIdentityOptions> _options;
    private readonly ILogger<VerifyEmailSentModel> _logger;

    private const string RateLimitCookieName = "verify_sent_at";
    private const int RateLimitSeconds = 60;

    [BindProperty(SupportsGet = true)]
    public string Email { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? RateLimitMessage { get; set; }

    public VerifyEmailSentModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<AqIdentityOptions> options,
        ILogger<VerifyEmailSentModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToPage("/Auth/Login");
        }

        Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email))
        {
            return RedirectToPage("/Auth/Login");
        }

        var cookie = Request.Cookies[RateLimitCookieName];
        if (!string.IsNullOrEmpty(cookie) && long.TryParse(cookie, out var lastSentTicks))
        {
            var lastSent = new DateTime(lastSentTicks, DateTimeKind.Utc);
            var secondsElapsed = (DateTime.UtcNow - lastSent).TotalSeconds;

            if (secondsElapsed < RateLimitSeconds)
            {
                var secondsRemaining = Math.Ceiling(RateLimitSeconds - secondsElapsed);
                RateLimitMessage = $"Please wait {secondsRemaining} second(s) before requesting another link.";
                return Page();
            }
        }

        var user = await _userManager.FindByEmailAsync(Email);
        if (user == null)
        {
            return RedirectToPage("/Auth/Login");
        }

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

            Response.Cookies.Append(
                RateLimitCookieName,
                DateTime.UtcNow.Ticks.ToString(),
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddSeconds(RateLimitSeconds + 1)
                });

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email for {Email}", Email);
            ModelState.AddModelError(string.Empty, "An error occurred while sending the verification email. Please try again.");
            return Page();
        }
    }
}
