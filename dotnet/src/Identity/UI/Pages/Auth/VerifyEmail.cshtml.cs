using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

public class VerifyEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<VerifyEmailModel> _logger;

    public bool IsVerified { get; set; }
    public string? Email { get; set; }

    public VerifyEmailModel(UserManager<ApplicationUser> userManager, ILogger<VerifyEmailModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            return RedirectToPage("/Auth/Login");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            IsVerified = false;
            Email = null;
            return Page();
        }

        Email = user.Email;

        var result = await _userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded)
        {
            IsVerified = true;
        }
        else
        {
            IsVerified = false;
            _logger.LogWarning("Email confirmation failed for user {UserId}. Errors: {Errors}",
                userId,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Page();
    }
}
