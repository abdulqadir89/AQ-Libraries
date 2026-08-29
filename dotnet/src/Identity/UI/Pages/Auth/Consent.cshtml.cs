using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using AQ.Identity.Core.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.UI.Pages.Auth;

[Authorize]
public class ConsentModel(
    UserManager<ApplicationUser> userManager,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string ClientDisplayName { get; set; } = string.Empty;

    public List<string> ScopeDisplayNames { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var request = await GetPendingRequestAsync();
        if (request == null) return BadRequest();

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application details cannot be retrieved.");

        ClientDisplayName = await applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!;

        foreach (var scope in request.GetScopes())
        {
            if (scope == Scopes.OfflineAccess) continue;
            var descriptor = await scopeManager.FindByNameAsync(scope);
            ScopeDisplayNames.Add(descriptor != null
                ? await scopeManager.GetDisplayNameAsync(descriptor) ?? scope
                : scope);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAllowAsync()
    {
        var request = await GetPendingRequestAsync();
        if (request == null) return BadRequest();

        var user = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application details cannot be retrieved.");

        var authorization = await authorizationManager.CreateAsync(
            principal: new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()),
            subject: await userManager.GetUserIdAsync(user),
            client: (await applicationManager.GetIdAsync(application))!,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes());

        // Re-entering the original /connect/authorize request now finds the permanent
        // authorization just created and proceeds straight to sign-in.
        return LocalRedirectToOriginalRequest();
    }

    public IActionResult OnPostDeny()
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                    "The authorization was denied by the resource owner."
            }));
    }

    private IActionResult LocalRedirectToOriginalRequest()
    {
        if (string.IsNullOrEmpty(ReturnUrl) || !Url.IsLocalUrl(ReturnUrl))
        {
            return RedirectToPage("/Auth/Login");
        }

        return LocalRedirect(ReturnUrl);
    }

    /// <summary>
    /// The consent page is reached via a redirect, not by OpenIddict's own routing, so there's
    /// no ambient OpenIddict server request on this GET/POST — reconstruct it by replaying
    /// <see cref="ReturnUrl"/> (the original, fully-formed /connect/authorize request) against
    /// OpenIddict's request-parsing pipeline.
    /// </summary>
    private Task<OpenIddictRequest?> GetPendingRequestAsync()
    {
        if (string.IsNullOrEmpty(ReturnUrl) || !Url.IsLocalUrl(ReturnUrl))
            return Task.FromResult<OpenIddictRequest?>(null);

        var queryString = new Uri("http://localhost" + ReturnUrl).Query;
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);

        return Task.FromResult<OpenIddictRequest?>(new OpenIddictRequest(query));
    }
}
