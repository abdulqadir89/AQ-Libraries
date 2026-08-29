using System.Linq;
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

        var redirectUriError = ClientDescriptorBuilder.ValidateRedirectUris(
            req.RedirectUris.Concat(req.PostLogoutRedirectUris));
        if (redirectUriError != null)
        {
            ThrowError(redirectUriError);
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
}
