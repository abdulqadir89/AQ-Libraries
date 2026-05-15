using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class UpdateScopeEndpoint(IIdentityDbContext context) : Endpoint<UpdateIdentityScopeRequest, IdentityScopeDto>
{
    public override void Configure()
    {
        Put("/manage/scopes/{Id}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(UpdateIdentityScopeRequest req, CancellationToken ct)
    {
        var scope = await context.IdentityScopes
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct);

        if (scope is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        scope.Update(req.DisplayName, req.Description);

        // Replace claim types: remove old, insert new
        var existing = await context.ScopeClaimTypes
            .Where(sct => sct.ScopeId == req.Id)
            .ToListAsync(ct);
        context.ScopeClaimTypes.RemoveRange(existing);

        var newClaimTypes = req.ClaimTypes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(claimType => ScopeClaimType.Create(scope.Id, claimType))
            .ToList();
        context.ScopeClaimTypes.AddRange(newClaimTypes);

        await context.SaveChangesAsync(ct);

        var response = new IdentityScopeDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            ClaimTypes = newClaimTypes.Select(c => c.ClaimType).ToList()
        };

        await Send.OkAsync(response, ct);
    }
}
