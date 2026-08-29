using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Utilities.Email;

namespace AQ.Identity.UI.Pages.Auth;

public class ConfirmEmailChangeModel(
    UserManager<ApplicationUser> userManager,
    IIdentityDbContext context,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    IOptions<AqIdentityOptions> options,
    ILogger<ConfirmEmailChangeModel> logger) : PageModel
{
    public bool IsConfirmed { get; set; }
    public string? NewEmail { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? newEmail, string? code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(code))
        {
            return RedirectToPage("/Auth/Login");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            IsConfirmed = false;
            return Page();
        }

        var oldEmail = user.Email;
        NewEmail = newEmail;

        var result = await userManager.ChangeEmailAsync(user, newEmail, code);
        if (!result.Succeeded)
        {
            IsConfirmed = false;
            logger.LogWarning("Email change confirmation failed for user {UserId}. Errors: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Page();
        }

        // Email doubles as the username in this app's registration convention (see
        // RegisterModel) — keep them in sync so sign-in by email keeps working.
        await userManager.SetUserNameAsync(user, newEmail);

        IsConfirmed = true;

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.EmailChanged, user.Id, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        if (!string.IsNullOrEmpty(oldEmail))
        {
            try
            {
                var message = emailTemplateService.BuildSecurityAlertEmail(
                    oldEmail,
                    $"Your account's email address was changed to {newEmail}",
                    options.Value.AppName);
                await emailService.SendAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send email-change notice to old address for user {UserId}", user.Id);
            }
        }

        return Page();
    }
}
