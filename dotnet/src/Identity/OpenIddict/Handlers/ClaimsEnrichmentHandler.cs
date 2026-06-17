using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using System.Text.Json;

namespace AQ.Identity.OpenIddict.Handlers;

public class ClaimsEnrichmentHandler(
    IIdentityDbContext context,
    IOpenIddictScopeManager scopeManager,
    ILogger<ClaimsEnrichmentHandler> logger)
    : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignInContext>
{
    private const string ClaimTypesKey = "claim_types";

    public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessSignInContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var principal = ctx.AccessTokenPrincipal;
        if (principal is null) return;

        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId)) return;

        var grantedScopes = principal
            .FindAll(OpenIddictConstants.Claims.Private.Scope)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ctx.CancellationToken);

            if (user is null)
            {
                logger.LogWarning("No user found for subject {Subject} during claims enrichment", userId);
                return;
            }

            principal.SetClaim("stamp", user.SecurityStamp);

            // Resolve claim types declared in each granted scope's Properties JSON
            var claimTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var scopeName in grantedScopes)
            {
                var scope = await scopeManager.FindByNameAsync(scopeName, ctx.CancellationToken);
                if (scope is null) continue;

                var props = await scopeManager.GetPropertiesAsync(scope, ctx.CancellationToken);
                if (props.TryGetValue(ClaimTypesKey, out var val) && val is JsonElement el && el.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in el.EnumerateArray())
                    {
                        var ct = item.GetString();
                        if (!string.IsNullOrWhiteSpace(ct))
                            claimTypes.Add(ct);
                    }
                }
            }

            if (claimTypes.Count == 0) return;

            var storedClaims = await context.StoredClaims
                .AsNoTracking()
                .Where(c => c.UserId == userId && claimTypes.Contains(c.Type))
                .ToListAsync(ctx.CancellationToken);

            foreach (var claim in storedClaims)
            {
                principal.SetClaim(claim.Type, claim.Value);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Claims enrichment failed for subject {Subject}. Token will be issued without enriched claims", userId);
        }
    }
}
