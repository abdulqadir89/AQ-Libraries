using AQ.Identity.Core.Abstractions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class DeleteScopeRequest
{
    public Guid Id { get; set; }
}

public class DeleteScopeEndpoint(IIdentityDbContext context) : Endpoint<DeleteScopeRequest>
{
    public override void Configure()
    {
        Delete("/manage/scopes/{Id}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(DeleteScopeRequest req, CancellationToken ct)
    {
        var scope = await context.IdentityScopes
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct);

        if (scope is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var claimTypes = await context.ScopeClaimTypes
            .Where(sct => sct.ScopeId == req.Id)
            .ToListAsync(ct);
        context.ScopeClaimTypes.RemoveRange(claimTypes);
        context.IdentityScopes.Remove(scope);

        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
