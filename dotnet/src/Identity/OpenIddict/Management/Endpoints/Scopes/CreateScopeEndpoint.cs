using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class CreateScopeEndpoint(IIdentityDbContext context) : Endpoint<UpsertIdentityScopeRequest, IdentityScopeDto>
{
    public override void Configure()
    {
        Post("/manage/scopes");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(UpsertIdentityScopeRequest req, CancellationToken ct)
    {
        var exists = await context.IdentityScopes
            .AnyAsync(s => s.Name == req.Name, ct);

        if (exists)
        {
            AddError(r => r.Name, "A scope with this name already exists.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var scope = IdentityScope.Create(req.Name, req.DisplayName, req.Description);
        context.IdentityScopes.Add(scope);

        var claimTypes = req.ClaimTypes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ct2 => ScopeClaimType.Create(scope.Id, ct2))
            .ToList();
        context.ScopeClaimTypes.AddRange(claimTypes);

        await context.SaveChangesAsync(ct);

        var response = new IdentityScopeDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            ClaimTypes = claimTypes.Select(c => c.ClaimType).ToList()
        };

        await Send.CreatedAtAsync<GetAllScopesEndpoint>(null, response, cancellation: ct);
    }
}
