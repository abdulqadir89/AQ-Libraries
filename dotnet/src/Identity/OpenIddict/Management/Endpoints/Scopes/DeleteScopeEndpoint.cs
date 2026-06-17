using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class DeleteScopeRequest
{
    public string Id { get; set; } = string.Empty;
}

public class DeleteScopeEndpoint(IOpenIddictScopeManager scopeManager) : Endpoint<DeleteScopeRequest>
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

        await Send.NoContentAsync(ct);
    }
}
