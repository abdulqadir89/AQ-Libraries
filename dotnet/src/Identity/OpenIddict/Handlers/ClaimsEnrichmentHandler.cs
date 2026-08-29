using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;

namespace AQ.Identity.OpenIddict.Handlers;

public class ClaimsEnrichmentHandler(
    IIdentityDbContext context,
    IOpenIddictScopeManager scopeManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager,
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

        // Loaded and stamped outside the try/catch below: if this lookup fails, the
        // token must not be issued at all, rather than issued without a stamp claim
        // (a missing stamp bypasses the "revoke all sessions" check in the validation
        // handler, since a null tokenStamp is treated as valid there).
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ctx.CancellationToken);

        if (user is null)
        {
            logger.LogWarning("No user found for subject {Subject} during claims enrichment", userId);
            ctx.Reject(OpenIddictConstants.Errors.InvalidGrant, "User account no longer exists.");
            return;
        }

        principal.SetClaim("stamp", user.SecurityStamp);

        // Attach every token minted for this sign-in to one durable "session" — a
        // permanent OpenIddictAuthorization — instead of the ad-hoc one OpenIddict's
        // built-in AttachAuthorization handler already created on the principal by the
        // time this handler runs (it fires earlier in the ProcessSignInContext
        // pipeline). Only the very first sign-in (authorization_code grant) should
        // replace it: on a refresh-token grant, the incoming principal is rebuilt from
        // the redeemed refresh token and already carries the *original* permanent
        // authorization id from the prior code exchange — overwriting it here would
        // fork a new "session" on every hourly refresh, defeating the point. This is
        // what makes "Sessions" in the account UI mean one row per login/device rather
        // than one row per token reissue.
        if (ctx.Request?.GrantType == OpenIddictConstants.GrantTypes.AuthorizationCode)
        {
            // OpenIddict's built-in AttachAuthorization handler (runs earlier in this
            // same event) already created and stamped an ad-hoc authorization onto the
            // principal by this point. Capture its id so it can be discarded below —
            // otherwise it's left behind as a permanently "valid" but tokenless
            // authorization: harmless to token validation, but it inflates any count
            // or listing that only checks authorization status (like the "App
            // Sessions" tile) without also checking for a live token, the way the
            // Sessions page does.
            var discardedAuthorizationId = principal.GetAuthorizationId();

            // The "client" CreateAsync expects is the application's internal entity id,
            // not the oi_prst/ClientId claim value (e.g. "els-web") — passing the raw
            // client id string here left the authorization's ApplicationId empty, which
            // is why the Sessions page rendered every row as "Unknown App".
            var clientId = principal.GetClaim(OpenIddictConstants.Claims.Private.Presenter) ?? ctx.ClientId;
            var application = !string.IsNullOrEmpty(clientId)
                ? await applicationManager.FindByClientIdAsync(clientId, ctx.CancellationToken)
                : null;

            if (application != null)
            {
                var applicationId = await applicationManager.GetIdAsync(application, ctx.CancellationToken);
                var authorization = await authorizationManager.CreateAsync(
                    principal: principal,
                    subject: userId.ToString(),
                    client: applicationId!,
                    type: OpenIddictConstants.AuthorizationTypes.Permanent,
                    scopes: grantedScopes.ToImmutableArray(),
                    cancellationToken: ctx.CancellationToken);

                var authorizationId = await authorizationManager.GetIdAsync(authorization, ctx.CancellationToken);

                // AccessTokenPrincipal, RefreshTokenPrincipal and IdentityTokenPrincipal
                // are distinct ClaimsPrincipal instances — OpenIddict's own
                // AttachAuthorization handler stamps its ad-hoc authorization id onto
                // each of them individually, so this override has to do the same on
                // every one that's actually being issued, or some of this sign-in's
                // tokens end up linked to the discarded ad-hoc authorization instead.
                foreach (var tokenPrincipal in new[] { ctx.AccessTokenPrincipal, ctx.RefreshTokenPrincipal, ctx.IdentityTokenPrincipal })
                    tokenPrincipal?.SetAuthorizationId(authorizationId);

                if (!string.IsNullOrEmpty(discardedAuthorizationId) && discardedAuthorizationId != authorizationId)
                {
                    var discarded = await authorizationManager.FindByIdAsync(discardedAuthorizationId, ctx.CancellationToken);
                    if (discarded != null)
                        await authorizationManager.TryRevokeAsync(discarded, ctx.CancellationToken);
                }
            }
        }

        try
        {
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
