using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public class DeleteClientEndpoint(
    IOpenIddictApplicationManager applicationManager,
    IIdentityDbContext context)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/manage/clients/{ClientId}");
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

        await applicationManager.DeleteAsync(existing, ct);

        var auditEntry = AuditEntry.Log(
            AuditEntry.Actions.ClientDeleted,
            userId: null,
            ip: null,
            ua: null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
