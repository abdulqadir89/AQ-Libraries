using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class UpdateScopeEndpoint(IOpenIddictScopeManager scopeManager) : Endpoint<UpdateIdentityScopeRequest, IdentityScopeDto>
{
    public override void Configure()
    {
        Put("/manage/scopes/{Id}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(UpdateIdentityScopeRequest req, CancellationToken ct)
    {
        var scope = await scopeManager.FindByIdAsync(req.Id, ct);
        if (scope is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var name = await scopeManager.GetNameAsync(scope, ct) ?? string.Empty;

        var descriptor = CreateScopeEndpoint.BuildDescriptor(name, req.DisplayName, req.Description, req.ClaimTypes);
        await scopeManager.UpdateAsync(scope, descriptor, ct);

        var claimTypes = req.ClaimTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        await Send.OkAsync(new IdentityScopeDto
        {
            Id = req.Id,
            Name = name,
            DisplayName = req.DisplayName,
            Description = req.Description,
            ClaimTypes = claimTypes
        }, ct);
    }
}
