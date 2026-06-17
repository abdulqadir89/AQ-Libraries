using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using OpenIddict.Abstractions;
using System.Text.Json;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class CreateScopeEndpoint(IOpenIddictScopeManager scopeManager) : Endpoint<UpsertIdentityScopeRequest, IdentityScopeDto>
{
    public override void Configure()
    {
        Post("/manage/scopes");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(UpsertIdentityScopeRequest req, CancellationToken ct)
    {
        var existing = await scopeManager.FindByNameAsync(req.Name, ct);
        if (existing is not null)
        {
            AddError(r => r.Name, "A scope with this name already exists.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var descriptor = BuildDescriptor(req.Name, req.DisplayName, req.Description, req.ClaimTypes);
        var scope = await scopeManager.CreateAsync(descriptor, ct);

        var id = await scopeManager.GetIdAsync(scope, ct) ?? string.Empty;

        await Send.CreatedAtAsync<GetAllScopesEndpoint>(null, new IdentityScopeDto
        {
            Id = id,
            Name = req.Name,
            DisplayName = req.DisplayName,
            Description = req.Description,
            ClaimTypes = req.ClaimTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        }, cancellation: ct);
    }

    internal static OpenIddictScopeDescriptor BuildDescriptor(string name, string displayName, string description, IEnumerable<string> claimTypes)
    {
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        };

        var distinct = claimTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (distinct.Count > 0)
        {
            descriptor.Properties["claim_types"] = JsonSerializer.SerializeToElement(distinct);
        }

        return descriptor;
    }
}
