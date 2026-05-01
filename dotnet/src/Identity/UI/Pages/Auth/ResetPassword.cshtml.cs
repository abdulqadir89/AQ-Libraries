using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ResetPasswordModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string UserId { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = default!;

    [BindProperty]
    public string Password { get; set; } = default!;

    [BindProperty]
    public string ConfirmPassword { get; set; } = default!;

    public bool TokenInvalid { get; set; }

    public ResetPasswordModel(UserManager<ApplicationUser> userManager, ILogger<ResetPasswordModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            TokenInvalid = true;
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TokenInvalid = true;
            return Page();
        }

        UserId = userId;
        Token = code;
        TokenInvalid = false;

        return Page();
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

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
        {
            TokenInvalid = true;
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, Token, Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var fieldName = error.Code switch
                {
                    "InvalidToken" => "Token",
                    "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit" or "PasswordRequiresUpper" or "PasswordRequiresLower" => "Password",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(fieldName) && fieldName == "Token")
                {
                    TokenInvalid = true;
                    return Page();
                }

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

        return RedirectToPage("/Auth/ResetPasswordConfirmation");
    }
}
