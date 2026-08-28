using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AQ.Identity.UI.Pages;

public class ThemeModel : PageModel
{
    public const string CookieName = "aq-theme";
    private static readonly string[] ValidThemes = ["clay", "blue", "slate", "zinc", "bloom", "spark"];
    private static readonly string[] ValidModes = ["light", "dark"];

    public IActionResult OnPost(string? theme, string? mode, string? returnUrl)
    {
        var resolvedTheme = ValidThemes.Contains(theme) ? theme! : "clay";
        var resolvedMode = ValidModes.Contains(mode) ? mode! : "light";

        Response.Cookies.Append(CookieName, $"{resolvedTheme}:{resolvedMode}", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }
}
