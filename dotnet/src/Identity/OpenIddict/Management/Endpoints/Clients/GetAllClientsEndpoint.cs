using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public class GetAllClientsEndpoint(IOpenIddictApplicationManager applicationManager)
    : EndpointWithoutRequest<IEnumerable<ClientSummaryDto>>
{
    public override void Configure()
    {
        Get("/manage/clients");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var clients = new List<ClientSummaryDto>();

        var applications = applicationManager.ListAsync(count: int.MaxValue, offset: 0, cancellationToken: ct);
        await foreach (var app in applications)
        {
            var clientId = await applicationManager.GetClientIdAsync(app, ct);
            if (string.IsNullOrEmpty(clientId)) continue;

            var displayName = await applicationManager.GetDisplayNameAsync(app, ct);
            var clientType = await applicationManager.GetClientTypeAsync(app, ct);

            var redirectUris = await applicationManager.GetRedirectUrisAsync(app, ct);
            var redirectUriList = redirectUris.Select(u => u.ToString()).ToList();

            var scopes = new List<string>();
            var permissions = await applicationManager.GetPermissionsAsync(app, ct);
            var scopePrefix = "scp:";
            foreach (var permission in permissions)
            {
                if (permission.StartsWith(scopePrefix))
                {
                    scopes.Add(permission[scopePrefix.Length..]);
                }
            }

            clients.Add(new ClientSummaryDto
            {
                ClientId = clientId,
                DisplayName = displayName,
                ClientType = clientType,
                RedirectUris = redirectUriList,
                Scopes = scopes
            });
        }

        await Send.OkAsync(clients, ct);
    }
}
