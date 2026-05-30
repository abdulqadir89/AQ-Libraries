using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public class ResetClientSecretResponse
{
    public string ClientSecret { get; set; } = string.Empty;
}

public class ResetClientSecretEndpoint(
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context)
    : EndpointWithoutRequest<ResetClientSecretResponse>
{
    public override void Configure()
    {
        Post("/manage/clients/{ClientId}/reset-secret");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var clientId = Route<string>("ClientId")!;

        var existing = await applicationManager.FindByClientIdAsync(clientId, ct);
        if (existing == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var clientType = await applicationManager.GetClientTypeAsync(existing, ct);
        if (clientType != OpenIddictConstants.ClientTypes.Confidential)
        {
            ThrowError("Only confidential clients have secrets", statusCode: 400);
        }

        // Build updated descriptor preserving all existing settings
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, existing, ct);

        var newSecret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        descriptor.ClientSecret = newSecret;

        await applicationManager.UpdateAsync(existing, descriptor, ct);

        var auditEntry = AuditEntry.Log(
            AuditEntry.Actions.ClientSecretReset,
            userId: null,
            ip: null,
            ua: null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.OkAsync(new ResetClientSecretResponse { ClientSecret = newSecret }, ct);
    }
}
