namespace AQ.Identity.Core.Abstractions;

/// <summary>
/// Hook for host applications to inject custom claims into JWT tokens during issuance.
/// </summary>
public interface IClaimsEnricher
{
    /// <summary>
    /// Called during token issuance to enrich the JWT with additional claims.
    /// </summary>
    /// <param name="subject">The IdP's user ID (maps to ApplicationUser.Id)</param>
    /// <param name="clientId">The OIDC client application ID</param>
    /// <param name="scopes">The requested scopes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Additional claims to merge into the JWT. Return empty dictionary if no extra claims needed.</returns>
    /// <remarks>Never throw — return empty dict on error and log internally.</remarks>
    Task<IReadOnlyDictionary<string, string>> EnrichAsync(
        Guid subject,
        string clientId,
        IEnumerable<string> scopes,
        CancellationToken ct);
}
