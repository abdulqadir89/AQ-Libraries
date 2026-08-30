using AQ.Identity.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AQ.Identity.UI.Pages;

public class ThemeModel(IOptions<AqIdentityOptions> options) : PageModel
{
    public const string CookieName = "aq-theme";
    private static readonly string[] ValidThemes = ["clay", "blue", "slate", "zinc", "bloom", "spark"];
    private static readonly string[] ValidModes = ["light", "dark"];

    public IActionResult OnPost(string? theme, string? mode, string? returnUrl)
    {
        var defaultTheme = ValidThemes.Contains(options.Value.Branding.DefaultTheme)
            ? options.Value.Branding.DefaultTheme
            : "clay";
        var resolvedTheme = ValidThemes.Contains(theme) ? theme! : defaultTheme;
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
