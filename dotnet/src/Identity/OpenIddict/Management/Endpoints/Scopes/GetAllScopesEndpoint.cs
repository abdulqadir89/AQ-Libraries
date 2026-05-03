using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class GetAllScopesEndpoint(IIdentityDbContext context) : EndpointWithoutRequest<List<IdentityScopeDto>>
{
    public override void Configure()
    {
        Get("/manage/scopes");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var scopes = await context.IdentityScopes
            .AsNoTracking()
            .Select(s => new IdentityScopeDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                ClaimTypes = context.ScopeClaimTypes
                    .Where(sct => sct.ScopeId == s.Id)
                    .Select(sct => sct.ClaimType)
                    .ToList()
            })
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        await Send.OkAsync(scopes, ct);
    }
}
