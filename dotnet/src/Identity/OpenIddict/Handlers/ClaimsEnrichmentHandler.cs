using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Handlers;

public class ClaimsEnrichmentHandler(
    IIdentityDbContext context,
    ILogger<ClaimsEnrichmentHandler> logger)
    : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignInContext>
{
    public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessSignInContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var principal = ctx.Principal;
        if (principal is null) return;

        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId)) return;

        var grantedScopes = principal
            .FindAll(Claims.Scope)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Embed SecurityStamp so ValidateTokenContext can detect invalidation
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ctx.CancellationToken);

            if (user is null)
            {
                logger.LogWarning("No user found for subject {Subject} during claims enrichment", userId);
                return;
            }

            principal.SetClaim("stamp", user.SecurityStamp);

            // Resolve claim types declared by the granted scopes
            var claimTypes = await context.ScopeClaimTypes
                .AsNoTracking()
                .Where(sct => context.IdentityScopes
                    .Where(s => grantedScopes.Contains(s.Name))
                    .Select(s => s.Id)
                    .Contains(sct.ScopeId))
                .Select(sct => sct.ClaimType)
                .Distinct()
                .ToListAsync(ctx.CancellationToken);

            if (claimTypes.Count == 0) return;

            // Load stored claims for this user, filtered to the resolved claim types
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
