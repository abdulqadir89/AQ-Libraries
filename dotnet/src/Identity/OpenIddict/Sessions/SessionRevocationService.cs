using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Sessions;

/// <summary>
/// Ends app sessions — a session being one permanent OpenIddictAuthorization (see
/// ClaimsEnrichmentHandler) plus every token issued under it. Logout, the account Sessions
/// page, account deletion and the admin API all go through here, so signing out actually
/// stops the app's refresh token from working instead of only clearing the IdP cookie, and
/// every affected client gets a back-channel logout notification.
///
/// Every authorization created from one IdP sign-in carries that sign-in's browser session id
/// (<see cref="BrowserSession.SessionIdClaim"/>) in its properties. That is what lets one
/// logout end the sessions of every app the browser signed in to (single sign-out), while
/// leaving the same user's other devices alone.
/// </summary>
public class SessionRevocationService(
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictTokenManager tokenManager,
    BackchannelLogoutNotifier backchannelLogout,
    ILogger<SessionRevocationService> logger)
{
    /// <summary>
    /// Revokes everything a sign-out should end: every app session started from the browser
    /// session in the IdP cookie, plus the session the client named in its id_token_hint.
    /// The hint covers a browser whose IdP cookie has already expired, and sessions created
    /// before authorizations carried a browser session id. A hint for a different user than
    /// the cookie is ignored.
    /// </summary>
    public async Task RevokeForSignOutAsync(ClaimsPrincipal? browser, ClaimsPrincipal? idTokenHint, CancellationToken ct)
    {
        var ended = new List<BackchannelLogoutTarget>();

        var browserSubject = browser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var browserSessionId = browser?.FindFirst(BrowserSession.SessionIdClaim)?.Value;

        if (!string.IsNullOrEmpty(browserSubject) && !string.IsNullOrEmpty(browserSessionId))
        {
            await RevokeBrowserSessionAsync(browserSubject, browserSessionId, ended, ct);
        }

        var hintSubject = idTokenHint?.GetClaim(Claims.Subject);
        if (idTokenHint is not null && !string.IsNullOrEmpty(hintSubject))
        {
            if (!string.IsNullOrEmpty(browserSubject) && hintSubject != browserSubject)
            {
                logger.LogWarning(
                    "Sign-out id_token_hint subject {HintSubject} does not match the signed-in user {Subject}; hint ignored",
                    hintSubject, browserSubject);
            }
            else
            {
                var hintSessionId = idTokenHint.GetClaim(BrowserSession.SessionIdClaim);
                if (!string.IsNullOrEmpty(hintSessionId) && hintSessionId != browserSessionId)
                {
                    await RevokeBrowserSessionAsync(hintSubject, hintSessionId, ended, ct);
                }

                var hintAuthorizationId = idTokenHint.GetAuthorizationId();
                if (!string.IsNullOrEmpty(hintAuthorizationId))
                {
                    await RevokeCoreAsync(hintSubject, hintAuthorizationId, ended, ct);
                }
            }
        }

        await backchannelLogout.NotifyAsync(ended, ct);
    }

    /// <summary>
    /// Revokes one app session and its tokens. Returns false when the authorization doesn't
    /// exist, is already revoked or belongs to another user, so a caller can't revoke someone
    /// else's session.
    /// </summary>
    public async Task<bool> RevokeAuthorizationAsync(string subject, string authorizationId, CancellationToken ct)
    {
        var ended = new List<BackchannelLogoutTarget>();
        var revoked = await RevokeCoreAsync(subject, authorizationId, ended, ct);
        await backchannelLogout.NotifyAsync(ended, ct);
        return revoked;
    }

    /// <summary>Revokes every app session and token of a user, on every device.</summary>
    public async Task RevokeAllAsync(string subject, CancellationToken ct)
    {
        var ended = new List<BackchannelLogoutTarget>();
        foreach (var authorizationId in await FindValidAuthorizationIdsAsync(subject, sessionId: null, ct))
        {
            await RevokeCoreAsync(subject, authorizationId, ended, ct);
        }

        // Sweep anything not reached above (ad-hoc authorizations, tokens without one)
        await authorizationManager.RevokeBySubjectAsync(subject, ct);
        await tokenManager.RevokeBySubjectAsync(subject, ct);

        await backchannelLogout.NotifyAsync(ended, ct);
    }

    private async Task RevokeBrowserSessionAsync(string subject, string sessionId, List<BackchannelLogoutTarget> ended, CancellationToken ct)
    {
        foreach (var authorizationId in await FindValidAuthorizationIdsAsync(subject, sessionId, ct))
        {
            await RevokeCoreAsync(subject, authorizationId, ended, ct);
        }
    }

    private async Task<bool> RevokeCoreAsync(string subject, string authorizationId, List<BackchannelLogoutTarget> ended, CancellationToken ct)
    {
        var authorization = await authorizationManager.FindByIdAsync(authorizationId, ct);
        if (authorization is null) return false;
        if (await authorizationManager.GetSubjectAsync(authorization, ct) != subject) return false;
        if (await authorizationManager.GetStatusAsync(authorization, ct) != Statuses.Valid) return false;

        await authorizationManager.TryRevokeAsync(authorization, ct);

        // OpenIddict already rejects tokens of a revoked authorization at validation time;
        // revoking them as well keeps the token rows honest for anything that reads them directly.
        await tokenManager.RevokeByAuthorizationIdAsync(authorizationId, ct);

        var applicationId = await authorizationManager.GetApplicationIdAsync(authorization, ct);
        if (!string.IsNullOrEmpty(applicationId))
        {
            var properties = await authorizationManager.GetPropertiesAsync(authorization, ct);
            ended.Add(new BackchannelLogoutTarget(applicationId, subject, GetSessionId(properties)));
        }

        return true;
    }

    /// <summary>
    /// Ids of the subject's still-valid authorizations, optionally only those of one browser
    /// session. Collected first: revoking while the subject query is still streaming would
    /// update rows under an open reader.
    /// </summary>
    private async Task<List<string>> FindValidAuthorizationIdsAsync(string subject, string? sessionId, CancellationToken ct)
    {
        var ids = new List<string>();
        await foreach (var authorization in authorizationManager.FindBySubjectAsync(subject, ct))
        {
            if (await authorizationManager.GetStatusAsync(authorization, ct) != Statuses.Valid) continue;

            if (sessionId is not null)
            {
                var properties = await authorizationManager.GetPropertiesAsync(authorization, ct);
                if (GetSessionId(properties) != sessionId) continue;
            }

            ids.Add((await authorizationManager.GetIdAsync(authorization, ct))!);
        }

        return ids;
    }

    private static string? GetSessionId(IReadOnlyDictionary<string, JsonElement> properties) =>
        properties.TryGetValue(BrowserSession.SessionIdClaim, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
