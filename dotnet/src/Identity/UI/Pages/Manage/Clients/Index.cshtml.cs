using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace AQ.Identity.UI.Pages.Manage.Clients;

[Authorize(Policy = "ManageApi")]
public class ClientsIndexModel(IOpenIddictApplicationManager applicationManager) : PageModel
{
    public List<ClientRow> Clients { get; set; } = [];

    public async Task OnGetAsync()
    {
        var apps = applicationManager.ListAsync(count: int.MaxValue, offset: 0, cancellationToken: HttpContext.RequestAborted);
        await foreach (var app in apps)
        {
            var clientId = await applicationManager.GetClientIdAsync(app, HttpContext.RequestAborted);
            if (string.IsNullOrEmpty(clientId)) continue;

            var displayName = await applicationManager.GetDisplayNameAsync(app, HttpContext.RequestAborted);
            var clientType = await applicationManager.GetClientTypeAsync(app, HttpContext.RequestAborted);
            var permissions = await applicationManager.GetPermissionsAsync(app, HttpContext.RequestAborted);

            var grantType = permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials)
                ? "client_credentials"
                : "authorization_code";

            var scopes = permissions
                .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
                .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
                .ToList();

            var redirectUris = await applicationManager.GetRedirectUrisAsync(app, HttpContext.RequestAborted);

            Clients.Add(new ClientRow
            {
                ClientId = clientId,
                DisplayName = displayName ?? clientId,
                ClientType = clientType ?? "public",
                GrantType = grantType,
                Scopes = scopes,
                RedirectUriCount = redirectUris.Length
            });
        }

        Clients = [.. Clients.OrderBy(c => c.ClientId)];
    }

    public async Task<IActionResult> OnPostDeleteAsync(string clientId)
    {
        var existing = await applicationManager.FindByClientIdAsync(clientId, HttpContext.RequestAborted);
        if (existing != null)
            await applicationManager.DeleteAsync(existing, HttpContext.RequestAborted);

        TempData["Success"] = $"Client '{clientId}' has been deleted.";
        return RedirectToPage();
    }
}

public class ClientRow
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string GrantType { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = [];
    public int RedirectUriCount { get; set; }
}
