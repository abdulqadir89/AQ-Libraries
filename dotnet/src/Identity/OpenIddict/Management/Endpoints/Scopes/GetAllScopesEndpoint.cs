using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using OpenIddict.Abstractions;
using System.Text.Json;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Scopes;

public class GetAllScopesEndpoint(IOpenIddictScopeManager scopeManager) : EndpointWithoutRequest<List<IdentityScopeDto>>
{
    public override void Configure()
    {
        Get("/manage/scopes");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = new List<IdentityScopeDto>();

        await foreach (var scope in scopeManager.ListAsync(cancellationToken: ct))
        {
            var id = await scopeManager.GetIdAsync(scope, ct) ?? string.Empty;
            var name = await scopeManager.GetNameAsync(scope, ct) ?? string.Empty;
            var displayName = await scopeManager.GetDisplayNameAsync(scope, ct) ?? string.Empty;
            var description = await scopeManager.GetDescriptionAsync(scope, ct) ?? string.Empty;
            var props = await scopeManager.GetPropertiesAsync(scope, ct);

            var claimTypes = new List<string>();
            if (props.TryGetValue("claim_types", out var val) && val.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in val.EnumerateArray())
                {
                    var ct2 = item.GetString();
                    if (!string.IsNullOrWhiteSpace(ct2))
                        claimTypes.Add(ct2);
                }
            }

            result.Add(new IdentityScopeDto
            {
                Id = id,
                Name = name,
                DisplayName = displayName,
                Description = description,
                ClaimTypes = claimTypes
            });
        }

        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        await Send.OkAsync(result, ct);
    }
}
