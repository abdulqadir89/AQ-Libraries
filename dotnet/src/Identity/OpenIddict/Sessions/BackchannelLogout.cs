using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace AQ.Identity.OpenIddict.Sessions;

/// <summary>
/// OpenID Connect Back-Channel Logout 1.0 (https://openid.net/specs/openid-connect-backchannel-1_0.html)
/// names and client settings. OpenIddict 7 has no native support yet (planned for 8.0,
/// openiddict-core#2175), so the IdP side is implemented here to the spec, reusing OpenIddict's
/// signing credentials.
/// </summary>
public static class BackchannelLogout
{
    /// <summary>Client metadata (sec 2.2), stored in the OpenIddict application's properties.</summary>
    public const string UriProperty = "backchannel_logout_uri";
    public const string SessionRequiredProperty = "backchannel_logout_session_required";

    /// <summary>Discovery metadata (sec 2.1).</summary>
    public const string SupportedMetadata = "backchannel_logout_supported";
    public const string SessionSupportedMetadata = "backchannel_logout_session_supported";

    /// <summary>Logout token "events" member (sec 2.4) and explicit JWT type (sec 2.4 / RFC 8725 sec 3.11).</summary>
    public const string EventType = "http://schemas.openid.net/event/backchannel-logout";
    public const string TokenType = "logout+jwt";

    public static (string? Uri, bool SessionRequired) ReadClientSettings(ImmutableDictionary<string, JsonElement> properties)
    {
        var uri = properties.TryGetValue(UriProperty, out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null;
        var sessionRequired = properties.TryGetValue(SessionRequiredProperty, out var s) && s.ValueKind == JsonValueKind.True;
        return (uri, sessionRequired);
    }
}

/// <summary>One ended app session to report: the client, the user and the browser session id.</summary>
public sealed record BackchannelLogoutTarget(string ApplicationId, string Subject, string? SessionId);

/// <summary>
/// Sends logout tokens (sec 2.4) to the back-channel logout URI of each client whose session
/// ended (sec 2.5). Delivery is best effort with a short timeout, as the spec allows: a client
/// that misses it still can't refresh, because its tokens were already revoked.
/// </summary>
public class BackchannelLogoutNotifier(
    IOpenIddictApplicationManager applicationManager,
    IOptionsMonitor<OpenIddictServerOptions> serverOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<BackchannelLogoutNotifier> logger)
{
    public const string HttpClientName = "AQ.Identity.BackchannelLogout";

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(2);

    public virtual async Task NotifyAsync(IEnumerable<BackchannelLogoutTarget> targets, CancellationToken ct)
    {
        var sends = new List<Task>();
        foreach (var target in targets.Distinct())
        {
            var application = await applicationManager.FindByIdAsync(target.ApplicationId, ct);
            if (application is null) continue;

            var (uri, sessionRequired) = BackchannelLogout.ReadClientSettings(
                await applicationManager.GetPropertiesAsync(application, ct));
            if (string.IsNullOrEmpty(uri)) continue;

            var clientId = (await applicationManager.GetClientIdAsync(application, ct))!;

            // A client that requires sid can't act on a token without one (sec 2.2)
            if (sessionRequired && string.IsNullOrEmpty(target.SessionId))
            {
                logger.LogWarning("Back-channel logout skipped for {ClientId}: it requires sid and the session has none", clientId);
                continue;
            }

            sends.Add(SendAsync(uri, clientId, CreateLogoutToken(clientId, target), ct));
        }

        await Task.WhenAll(sends);
    }

    /// <summary>Logout token claims per sec 2.4: iss, aud, iat, exp, jti, events, sub and sid; never nonce.</summary>
    public string CreateLogoutToken(string clientId, BackchannelLogoutTarget target)
    {
        var options = serverOptions.CurrentValue;
        var now = DateTime.UtcNow;

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [JwtRegisteredClaimNames.Sub] = target.Subject,
            ["events"] = new Dictionary<string, object> { [BackchannelLogout.EventType] = new Dictionary<string, object>() },
        };
        if (!string.IsNullOrEmpty(target.SessionId))
        {
            claims[BrowserSession.SessionIdClaim] = target.SessionId;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer!.AbsoluteUri,
            Audience = clientId,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(TokenLifetime),
            Claims = claims,
            TokenType = BackchannelLogout.TokenType,
            // Same key OpenIddict signs identity tokens with, so clients validate it against the published JWKS
            SigningCredentials = options.SigningCredentials.First(),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private async Task SendAsync(string uri, string clientId, string logoutToken, CancellationToken ct)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("logout_token", logoutToken)]);
            using var response = await client.PostAsync(uri, content, ct);

            if (response.IsSuccessStatusCode)
                logger.LogInformation("Back-channel logout delivered to {ClientId}", clientId);
            else
                logger.LogWarning("Back-channel logout to {ClientId} failed with HTTP {StatusCode}", clientId, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Back-channel logout to {ClientId} could not be delivered", clientId);
        }
    }
}
