using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AQ.Identity.UI.Pages.Auth;

/// <summary>
/// Kicks off the Google OAuth redirect. The Login page's "Continue with Google"
/// button posts here; the provider then redirects back to /auth/external-callback.
/// </summary>
public class GoogleChallengeModel : PageModel
{
    public IActionResult OnGet(string? returnUrl)
    {
        var redirectUrl = Url.Page("/Auth/ExternalCallback", pageHandler: null, values: new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
}
