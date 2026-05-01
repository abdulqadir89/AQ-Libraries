using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Account;

[Authorize]
public class SecurityModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public bool TwoFactorEnabled { get; set; }

    public SecurityModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        TwoFactorEnabled = user.TwoFactorEnabled;

        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(
        string CurrentPassword,
        string NewPassword,
        string ConfirmNewPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            TwoFactorEnabled = user.TwoFactorEnabled;
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            TwoFactorEnabled = user.TwoFactorEnabled;
            return Page();
        }

        TempData["Success"] = "Your password has been changed successfully.";
        return RedirectToPage();
    }
}
