using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public class UpdateClientEndpoint(
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context)
    : Endpoint<IdentityClientConfig>
{
    public override void Configure()
    {
        Put("/manage/clients/{ClientId}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(IdentityClientConfig req, CancellationToken ct)
    {
        var clientId = Route<string>("ClientId")!;

        // Validate redirect URIs
        foreach (var redirectUri in req.RedirectUris)
        {
            if (!IsValidRedirectUri(redirectUri))
            {
                ThrowError($"Invalid redirect URI: {redirectUri}. HTTPS required, or HTTP for localhost/127.0.0.1");
            }
        }

        // Find existing client
        var existing = await applicationManager.FindByClientIdAsync(clientId, ct);
        if (existing == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Only allow updating if ClientId matches
        if (req.ClientId != clientId)
        {
            ThrowError("ClientId in body must match ClientId in URL");
        }

        var descriptor = ClientDescriptorBuilder.Build(req);
        await applicationManager.UpdateAsync(existing, descriptor, ct);

        var auditEntry = AuditEntry.Log(
            AuditEntry.Actions.ClientUpdated,
            userId: null,
            ip: null,
            ua: null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.OkAsync(ct);
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
