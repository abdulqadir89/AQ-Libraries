using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class DeleteScopeRequest
{
    public string Id { get; set; } = string.Empty;
}

public class DeleteScopeEndpoint(IOpenIddictScopeManager scopeManager, IIdentityDbContext context)
    : Endpoint<DeleteScopeRequest>
{
    public override void Configure()
    {
        Delete("/manage/scopes/{Id}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(DeleteScopeRequest req, CancellationToken ct)
    {
        var scope = await scopeManager.FindByIdAsync(req.Id, ct);
        if (scope is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await scopeManager.DeleteAsync(scope, ct);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.ScopeDeleted, userId: null, ip: null, ua: null));
        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
