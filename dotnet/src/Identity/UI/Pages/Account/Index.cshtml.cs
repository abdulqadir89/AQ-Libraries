using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.UI.Resources;
using AQ.Utilities.Email;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class AccountIndexModel(
    UserManager<ApplicationUser> userManager,
    IIdentityDbContext context,
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    IOptions<AqIdentityOptions> options,
    IStringLocalizer<IdentityUIResource> localizer,
    ILogger<AccountIndexModel> logger) : PageModel
{
    public ApplicationUser CurrentUser { get; set; } = default!;
    public bool TwoFactorEnabled { get; set; }
    public int ActiveSessionCount { get; set; }
    public int ConnectedAppCount { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool HasManageAccess { get; set; }

    private const string RateLimitCookieName = "verify_sent_at";
    private const int RateLimitSeconds = 60;

    public async Task<IActionResult> OnPostSendVerificationAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        if (user.EmailConfirmed)
        {
            TempData["AccountSuccess"] = localizer["Your email address is already verified."].Value;
            return RedirectToPage();
        }

        var cookie = Request.Cookies[RateLimitCookieName];
        if (long.TryParse(cookie, out var lastSentTicks))
        {
            var secondsElapsed = (DateTime.UtcNow - new DateTime(lastSentTicks, DateTimeKind.Utc)).TotalSeconds;
            if (secondsElapsed < RateLimitSeconds)
            {
                TempData["AccountError"] = localizer["Please wait {0} second(s) before requesting another link.", Math.Ceiling(RateLimitSeconds - secondsElapsed)].Value;
                return RedirectToPage();
            }
        }

        try
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var issuer = options.Value.Issuer ?? "http://localhost:5001";
            var verificationUrl = $"{issuer}/auth/verify-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(token)}";

            await emailService.SendAsync(emailTemplateService.BuildVerificationEmail(user.Email!, verificationUrl, options.Value.AppName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email for user {UserId}", user.Id);
            TempData["AccountError"] = localizer["We couldn't send the verification email. Please try again later."].Value;
            return RedirectToPage();
        }

        Response.Cookies.Append(
            RateLimitCookieName,
            DateTime.UtcNow.Ticks.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddSeconds(RateLimitSeconds + 1)
            });

        TempData["AccountSuccess"] = localizer["We sent a verification link to {0}. Check your inbox and spam folder.", user.Email!].Value;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Auth/Login");

        CurrentUser = user;
        TwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        LastLoginAt = user.LastLoginAt;

        // A "session" is one valid OpenIddictAuthorization with at least one live
        // token — created once at login and reused across every refresh-token grant
        // (see ClaimsEnrichmentHandler) — not one per token. Counting tokens here
        // double/triple counted (an access token reissues hourly against the same
        // login) and previously showed hundreds of "sessions" for a single signed-in
        // browser. The live-token check (matching the Sessions page) additionally
        // excludes ad-hoc authorizations OpenIddict's own pipeline creates and
        // discards before ClaimsEnrichmentHandler replaces them — those stay "valid"
        // but never have a token attached, and existed for logins that happened
        // before that discard-cleanup was added.
        var validTokenAuthorizationIds = new HashSet<string>();
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var token in tokens)
        {
            var tokenStatus = await tokenManager.GetStatusAsync(token, HttpContext.RequestAborted);
            if (tokenStatus != OpenIddictConstants.Statuses.Valid) continue;

            var tokenAuthId = await tokenManager.GetAuthorizationIdAsync(token, HttpContext.RequestAborted);
            if (!string.IsNullOrEmpty(tokenAuthId)) validTokenAuthorizationIds.Add(tokenAuthId);
        }

        var sessionCount = 0;
        var connectedClients = new HashSet<string?>();
        var authorizations = authorizationManager.FindBySubjectAsync(user.Id.ToString(), HttpContext.RequestAborted);
        await foreach (var authorization in authorizations)
        {
            var status = await authorizationManager.GetStatusAsync(authorization, HttpContext.RequestAborted);
            if (status != OpenIddictConstants.Statuses.Valid) continue;

            var authorizationId = await authorizationManager.GetIdAsync(authorization, HttpContext.RequestAborted);
            if (authorizationId is null || !validTokenAuthorizationIds.Contains(authorizationId)) continue;

            sessionCount++;

            var appId = await authorizationManager.GetApplicationIdAsync(authorization, HttpContext.RequestAborted);
            if (appId != null) connectedClients.Add(appId);
        }
        ActiveSessionCount = sessionCount;
        ConnectedAppCount = connectedClients.Count;

        // Check if user has manage_api claim
        HasManageAccess = await context.StoredClaims
            .AsNoTracking()
            .AnyAsync(c => c.UserId == user.Id && c.Type == "manage_api", HttpContext.RequestAborted);

        return Page();
    }
}
