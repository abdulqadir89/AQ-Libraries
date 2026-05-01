using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public class CreateClientEndpoint(
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context)
    : Endpoint<IdentityClientConfig>
{
    public override void Configure()
    {
        Post("/manage/clients");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(IdentityClientConfig req, CancellationToken ct)
    {
        // Validate redirect URIs
        foreach (var redirectUri in req.RedirectUris)
        {
            if (!IsValidRedirectUri(redirectUri))
            {
                ThrowError($"Invalid redirect URI: {redirectUri}. HTTPS required, or HTTP for localhost/127.0.0.1");
            }
        }

        // Check for duplicate
        var existing = await applicationManager.FindByClientIdAsync(req.ClientId, ct);
        if (existing != null)
        {
            ThrowError($"Client '{req.ClientId}' already exists", statusCode: 409);
        }

        var descriptor = ClientDescriptorBuilder.Build(req);
        await applicationManager.CreateAsync(descriptor, ct);

        var auditEntry = AuditEntry.Log(
            AuditEntry.Actions.ClientCreated,
            userId: null,
            ip: null,
            ua: null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.CreatedAtAsync<GetAllClientsEndpoint>(null, null, cancellation: ct);
    }

    private static bool IsValidRedirectUri(string raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme == Uri.UriSchemeHttps) return true;
        if (uri.Scheme == Uri.UriSchemeHttp)
            return uri.Host is "localhost" or "127.0.0.1" or "[::1]";
        return false;
    }
}
