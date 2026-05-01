using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.UI.Pages.Mfa;

[Authorize]
public class BackupCodesModel : PageModel
{
    public List<string> BackupCodes { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (TempData["BackupCodes"] is not string codesData || string.IsNullOrEmpty(codesData))
        {
            ErrorMessage = "Backup codes are only shown once. Go to Security settings to regenerate them.";
            return Page();
        }

        BackupCodes = codesData.Split(",").ToList();
        return Page();
    }

    public IActionResult OnPost()
    {
        if (TempData["BackupCodes"] is not string codesData || string.IsNullOrEmpty(codesData))
        {
            return RedirectToPage("/Account/Security");
        }

        TempData.Remove("BackupCodes");
        return RedirectToPage("/Account/Security");
    }
}
