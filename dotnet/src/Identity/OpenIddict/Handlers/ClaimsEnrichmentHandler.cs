using AQ.Identity.Core.Abstractions;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Handlers;

public class ClaimsEnrichmentHandler : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignInContext>
{
    private readonly IClaimsEnricher _enricher;
    private readonly ILogger<ClaimsEnrichmentHandler> _logger;

    public ClaimsEnrichmentHandler(
        IClaimsEnricher enricher,
        ILogger<ClaimsEnrichmentHandler> logger)
    {
        _enricher = enricher;
        _logger = logger;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessSignInContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.Principal;
        if (principal == null)
        {
            return;
        }

        var subjectClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? principal.FindFirst("sub");
        if (subjectClaim == null || !Guid.TryParse(subjectClaim.Value, out var subject))
        {
            return;
        }

        var clientId = context.Principal?.FindFirst(Claims.ClientId)?.Value;
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        var grantedScopes = context.Principal
            ?.FindAll(Claims.Scope)
            .Select(c => c.Value)
            .ToList() ?? [];

        try
        {
            var enrichedClaims = await _enricher.EnrichAsync(subject, clientId, grantedScopes, context.CancellationToken);

            foreach (var (key, value) in enrichedClaims)
            {
                principal.SetClaim(key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claims enrichment failed for subject {Subject} and client {ClientId}. Token will be issued without enriched claims.", subject, clientId);
        }
    }
}
