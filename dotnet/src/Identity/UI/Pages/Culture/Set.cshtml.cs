using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AQ.Identity.UI.Pages.Culture;

/// <summary>
/// Target of the language <c>&lt;select&gt;</c> in <c>_AuthLayout.cshtml</c>. Sets the
/// culture cookie the same way <see cref="Localization.ApplicationBuilderExtensions.UseAqIdentityLocalization"/>'s
/// middleware does, then redirects back to the page the switcher was used on.
/// </summary>
public class SetModel : PageModel
{
    private static readonly string[] SupportedCultures = ["en", "zh-CN", "zh-TW", "zh-HK"];

    public IActionResult OnPost(string culture, string? returnUrl)
    {
        if (Array.IndexOf(SupportedCultures, culture) < 0)
        {
            culture = "en";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                SameSite = SameSiteMode.Lax,
                HttpOnly = true,
                Secure = Request.IsHttps,
            });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "~/");
    }
}
