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
        if (existing == null) return NotFound();

        if (await WouldRemoveLastManageApiClientAsync(clientId, HttpContext.RequestAborted))
        {
            TempData["Error"] = $"Cannot delete '{clientId}' — it's the only client granted the manage_api scope. Deleting it would lock every admin out of this page.";
            return RedirectToPage();
        }

        await applicationManager.DeleteAsync(existing, HttpContext.RequestAborted);

        TempData["Success"] = $"Client '{clientId}' has been deleted.";
        return RedirectToPage();
    }

    /// <summary>
    /// Mirrors AdminClaimGuard's "last administrator" protection, but for the client side of
    /// manage_api: if this is the only client with the manage_api scope granted, deleting it
    /// would mean no client could ever mint a token carrying that scope again, stranding
    /// every admin out of /manage even though their manage_api claim is still intact.
    /// </summary>
    private async Task<bool> WouldRemoveLastManageApiClientAsync(string clientIdToDelete, CancellationToken ct)
    {
        const string manageApiPermission = OpenIddictConstants.Permissions.Prefixes.Scope + "manage_api";

        await foreach (var app in applicationManager.ListAsync(cancellationToken: ct))
        {
            var otherClientId = await applicationManager.GetClientIdAsync(app, ct);
            if (string.Equals(otherClientId, clientIdToDelete, StringComparison.Ordinal)) continue;

            var permissions = await applicationManager.GetPermissionsAsync(app, ct);
            if (permissions.Contains(manageApiPermission)) return false;
        }

        var deletingClientPermissions = (await applicationManager.FindByClientIdAsync(clientIdToDelete, ct)) is { } app2
            ? await applicationManager.GetPermissionsAsync(app2, ct)
            : [];

        return deletingClientPermissions.Contains(manageApiPermission);
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
