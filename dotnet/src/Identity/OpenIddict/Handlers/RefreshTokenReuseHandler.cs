using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Handlers;

/// <summary>
/// Detects refresh token reuse and revokes the whole authorization family in response, per
/// OAuth 2.0 Security Best Current Practice sec 4.13.2 / RFC 6819 sec 5.2.2.3: a rotated-out
/// (already-redeemed) refresh token being presented again is a leakage signal — the credential
/// was likely copied by an attacker while the legitimate client had already rotated past it.
///
/// Rolling refresh tokens (one-time use, enabled by default in this OpenIddict version — see
/// <see cref="OpenIddict.Server.OpenIddictServerOptions.DisableRollingRefreshTokens"/>) already
/// mark a redeemed token as unusable and OpenIddict's own <c>ValidateTokenEntry</c> handler
/// rejects the grant; this handler only adds the revoke-the-family reaction on top, since
/// OpenIddict doesn't do that automatically. It must run BEFORE the built-in validation
/// handlers so it can inspect the token's status while <see cref="OpenIddictServerEvents.ProcessAuthenticationContext.RefreshTokenPrincipal"/>
/// is still populated with the extracted (but not yet entry-validated) principal.
/// </summary>
public class RefreshTokenReuseHandler(
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IIdentityDbContext context,
    ILogger<RefreshTokenReuseHandler> logger)
    : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessAuthenticationContext>
{
    public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessAuthenticationContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (ctx.Request is not { } request || !request.IsRefreshTokenGrantType()) return;

        var tokenId = ctx.RefreshTokenPrincipal?.GetTokenId();
        if (string.IsNullOrEmpty(tokenId)) return;

        var token = await tokenManager.FindByIdAsync(tokenId, ctx.CancellationToken);
        if (token is null) return;

        var status = await tokenManager.GetStatusAsync(token, ctx.CancellationToken);
        if (status != Statuses.Redeemed) return;

        var authorizationId = await tokenManager.GetAuthorizationIdAsync(token, ctx.CancellationToken);
        if (!string.IsNullOrEmpty(authorizationId))
        {
            var authorization = await authorizationManager.FindByIdAsync(authorizationId, ctx.CancellationToken);
            if (authorization != null)
                await authorizationManager.TryRevokeAsync(authorization, ctx.CancellationToken);
        }

        var subject = await tokenManager.GetSubjectAsync(token, ctx.CancellationToken);
        var userId = Guid.TryParse(subject, out var parsed) ? parsed : (Guid?)null;

        logger.LogWarning(
            "Refresh token reuse detected for subject {Subject} — authorization {AuthorizationId} revoked.",
            subject, authorizationId);

        context.AuditLog.Add(AuditEntry.Log(
            AuditEntry.Actions.RefreshTokenReuseDetected,
            userId: userId,
            ip: null,
            ua: null));
        await context.SaveChangesAsync(ctx.CancellationToken);
    }
}
