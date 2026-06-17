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
public class CreateClientModel(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IIdentityDbContext context) : PageModel
{
    [BindProperty] public string ClientId { get; set; } = string.Empty;
    [BindProperty] public string DisplayName { get; set; } = string.Empty;
    [BindProperty] public string Type { get; set; } = "public";
    [BindProperty] public string GrantType { get; set; } = "authorization_code";
    [BindProperty] public string? ClientSecret { get; set; }
    [BindProperty] public bool RequirePkce { get; set; }
    [BindProperty] public List<string> SelectedScopes { get; set; } = [];
    [BindProperty] public string RedirectUrisRaw { get; set; } = string.Empty;
    [BindProperty] public string PostLogoutUrisRaw { get; set; } = string.Empty;
    [BindProperty] public string ServiceAccountClaimsRaw { get; set; } = string.Empty;

    public List<ScopeOption> AvailableScopes { get; set; } = [];

    public async Task OnGetAsync()
    {
        AvailableScopes = await LoadScopesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        AvailableScopes = await LoadScopesAsync();

        if (Type == "confidential" && string.IsNullOrWhiteSpace(ClientSecret))
            ModelState.AddModelError(nameof(ClientSecret), "Client secret is required for confidential clients.");

        if (!ModelState.IsValid)
            return Page();

        var existing = await applicationManager.FindByClientIdAsync(ClientId, HttpContext.RequestAborted);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(ClientId), $"A client with ID '{ClientId}' already exists.");
            return Page();
        }

        var config = BuildConfig();
        var validationError = ValidateRedirectUris(config.RedirectUris);
        if (validationError != null)
        {
            ModelState.AddModelError(nameof(RedirectUrisRaw), validationError);
            return Page();
        }

        var descriptor = ClientDescriptorBuilder.Build(config);
        await applicationManager.CreateAsync(descriptor, HttpContext.RequestAborted);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ClientCreated, null, null, null));
        await context.SaveChangesAsync(HttpContext.RequestAborted);

        if (Type == "confidential" && !string.IsNullOrEmpty(ClientSecret))
        {
            TempData["NewClientId"] = ClientId;
            TempData["NewSecret"] = ClientSecret;
            return RedirectToPage("./SecretCreated");
        }

        TempData["Success"] = $"Client '{ClientId}' has been created.";
        return RedirectToPage("./Index");
    }

    private IdentityClientConfig BuildConfig()
    {
        var redirectUris = ParseLines(RedirectUrisRaw);
        var postLogoutUris = ParseLines(PostLogoutUrisRaw);
        var serviceAccountClaims = ParseKeyValues(ServiceAccountClaimsRaw);

        return new IdentityClientConfig
        {
            ClientId = ClientId,
            DisplayName = DisplayName,
            Type = Type,
            ClientSecret = Type == "confidential" ? ClientSecret : null,
            GrantType = GrantType,
            RequirePkce = RequirePkce,
            Scopes = SelectedScopes,
            RedirectUris = redirectUris,
            PostLogoutRedirectUris = postLogoutUris,
            ServiceAccountClaims = serviceAccountClaims
        };
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

    private async Task<List<ScopeOption>> LoadScopesAsync()
    {
        var result = new List<ScopeOption>();
        await foreach (var scope in scopeManager.ListAsync(cancellationToken: HttpContext.RequestAborted))
        {
            var name = await scopeManager.GetNameAsync(scope, HttpContext.RequestAborted);
            if (name is null) continue;
            var displayName = await scopeManager.GetDisplayNameAsync(scope, HttpContext.RequestAborted);
            result.Add(new ScopeOption(name, displayName ?? name));
        }
        return [.. result.OrderBy(s => s.Name)];
    }

    private static string? ValidateRedirectUris(List<string> uris)
    {
        foreach (var raw in uris)
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
                return $"'{raw}' is not a valid URI.";
            if (uri.Scheme == Uri.UriSchemeHttps) continue;
            if (uri.Scheme == Uri.UriSchemeHttp && uri.Host is "localhost" or "127.0.0.1" or "[::1]") continue;
            // Allow custom schemes (mobile apps)
            if (uri.Scheme != Uri.UriSchemeHttp) continue;
            return $"'{raw}': HTTP is only allowed for localhost.";
        }
        return null;
    }
}

public record ScopeOption(string Name, string DisplayName);
