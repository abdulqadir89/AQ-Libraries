using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Management.Endpoints.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Manage.Clients;

[Authorize(Policy = "ManageApi")]
public class EditClientModel(
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context) : PageModel
{
    [BindProperty(SupportsGet = true)] public string ClientId { get; set; } = string.Empty;
    [BindProperty] public string DisplayName { get; set; } = string.Empty;
    [BindProperty] public string Type { get; set; } = "public";
    [BindProperty] public string GrantType { get; set; } = "authorization_code";
    [BindProperty] public bool RequirePkce { get; set; }
    [BindProperty] public List<string> SelectedScopes { get; set; } = [];
    [BindProperty] public string RedirectUrisRaw { get; set; } = string.Empty;
    [BindProperty] public string PostLogoutUrisRaw { get; set; } = string.Empty;
    [BindProperty] public string ServiceAccountClaimsRaw { get; set; } = string.Empty;

    public List<IdentityScope> AvailableScopes { get; set; } = [];
    public bool IsConfidential { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        AvailableScopes = await context.IdentityScopes.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var existing = await applicationManager.FindByClientIdAsync(ClientId, HttpContext.RequestAborted);
        if (existing == null) return NotFound();

        var clientType = await applicationManager.GetClientTypeAsync(existing, HttpContext.RequestAborted);
        var permissions = await applicationManager.GetPermissionsAsync(existing, HttpContext.RequestAborted);
        var redirectUris = await applicationManager.GetRedirectUrisAsync(existing, HttpContext.RequestAborted);
        var postLogoutUris = await applicationManager.GetPostLogoutRedirectUrisAsync(existing, HttpContext.RequestAborted);
        var requirements = await applicationManager.GetRequirementsAsync(existing, HttpContext.RequestAborted);

        DisplayName = await applicationManager.GetDisplayNameAsync(existing, HttpContext.RequestAborted) ?? ClientId;
        Type = clientType ?? "public";
        IsConfidential = Type == OpenIddictConstants.ClientTypes.Confidential;

        GrantType = permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials)
            ? "client_credentials"
            : "authorization_code";

        RequirePkce = requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

        SelectedScopes = permissions
            .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
            .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .ToList();

        RedirectUrisRaw = string.Join(Environment.NewLine, redirectUris);
        PostLogoutUrisRaw = string.Join(Environment.NewLine, postLogoutUris);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        AvailableScopes = await context.IdentityScopes.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var existing = await applicationManager.FindByClientIdAsync(ClientId, HttpContext.RequestAborted);
        if (existing == null) return NotFound();

        var clientType = await applicationManager.GetClientTypeAsync(existing, HttpContext.RequestAborted);
        IsConfidential = clientType == OpenIddictConstants.ClientTypes.Confidential;
        Type = clientType ?? "public";

        if (!ModelState.IsValid) return Page();

        var redirectUris = ParseLines(RedirectUrisRaw);
        var validationError = ValidateRedirectUris(redirectUris);
        if (validationError != null)
        {
            ModelState.AddModelError(nameof(RedirectUrisRaw), validationError);
            return Page();
        }

        var config = new IdentityClientConfig
        {
            ClientId = ClientId,
            DisplayName = DisplayName,
            Type = Type,
            GrantType = GrantType,
            RequirePkce = RequirePkce,
            Scopes = SelectedScopes,
            RedirectUris = redirectUris,
            PostLogoutRedirectUris = ParseLines(PostLogoutUrisRaw),
            ServiceAccountClaims = ParseKeyValues(ServiceAccountClaimsRaw)
        };

        var descriptor = ClientDescriptorBuilder.Build(config);

        // Don't set ClientSecret here — null preserves the existing stored hash.
        // Secret resets are handled separately via OnPostResetSecretAsync.
        descriptor.ClientSecret = null;

        await applicationManager.UpdateAsync(existing, descriptor, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ClientUpdated, null, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["Success"] = $"Client '{ClientId}' has been updated.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostResetSecretAsync()
    {
        var existing = await applicationManager.FindByClientIdAsync(ClientId, HttpContext.RequestAborted);
        if (existing == null) return NotFound();

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, existing, HttpContext.RequestAborted);

        var newSecret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        descriptor.ClientSecret = newSecret;

        await applicationManager.UpdateAsync(existing, descriptor, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ClientSecretReset, null, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        TempData["NewClientId"] = ClientId;
        TempData["NewSecret"] = newSecret;
        return RedirectToPage("./SecretCreated");
    }

    private static List<string> ParseLines(string raw) =>
        raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Where(s => !string.IsNullOrEmpty(s))
           .Distinct()
           .ToList();

    private static Dictionary<string, string> ParseKeyValues(string raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in ParseLines(raw))
        {
            var idx = line.IndexOf('=');
            if (idx > 0)
                result[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }
        return result;
    }

    private static string? ValidateRedirectUris(List<string> uris)
    {
        foreach (var raw in uris)
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
                return $"'{raw}' is not a valid URI.";
            if (uri.Scheme == Uri.UriSchemeHttps) continue;
            if (uri.Scheme == Uri.UriSchemeHttp && uri.Host is "localhost" or "127.0.0.1" or "[::1]") continue;
            if (uri.Scheme != Uri.UriSchemeHttp) continue;
            return $"'{raw}': HTTP is only allowed for localhost.";
        }
        return null;
    }
}
