using AQ.Identity.Core.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

public static class ClientDescriptorBuilder
{
    /// <summary>
    /// Validates a redirect/post-logout URI: HTTPS is always allowed, HTTP only for
    /// loopback hosts (dev), and any other scheme (e.g. a mobile app's custom scheme
    /// like msauth.com.els.mobile://auth) is allowed since it can't be intercepted
    /// the way an HTTP redirect on a shared host could be.
    /// </summary>
    public static string? ValidateRedirectUri(string raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return $"'{raw}' is not a valid URI.";
        }

        if (uri.Scheme == Uri.UriSchemeHttps) return null;
        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            // host.docker.internal is Docker Desktop's loopback-equivalent hostname for
            // reaching the host machine from inside a container — used by containerized
            // dev/test setups the same way localhost is used natively.
            return uri.Host is "localhost" or "127.0.0.1" or "[::1]" or "host.docker.internal"
                ? null
                : $"'{raw}': HTTP is only allowed for localhost/127.0.0.1/host.docker.internal.";
        }

        // Non-HTTP(S) schemes (custom app schemes) are allowed unconditionally.
        return null;
    }

    public static string? ValidateRedirectUris(IEnumerable<string> uris) =>
        uris.Select(ValidateRedirectUri).FirstOrDefault(error => error != null);

    /// <summary>
    /// Builds the OpenIddict application descriptor. Throws <see cref="ArgumentException"/>
    /// if any redirect or post-logout URI fails <see cref="ValidateRedirectUri"/> — this
    /// applies to every caller, including config-seeded clients via ClientSeeder, so a
    /// malformed URI in appsettings.json fails loudly instead of throwing deep inside
    /// OpenIddict or silently registering an unsafe redirect target.
    /// </summary>
    public static OpenIddictApplicationDescriptor Build(IdentityClientConfig config)
    {
        foreach (var uri in config.RedirectUris.Concat(config.PostLogoutRedirectUris))
        {
            var error = ValidateRedirectUri(uri);
            if (error != null)
            {
                throw new ArgumentException($"Invalid URI for client '{config.ClientId}': {error}");
            }
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = config.ClientId,
            DisplayName = config.DisplayName,
            ClientType = config.Type,
        };

        if (config.RequirePkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        if (!string.IsNullOrEmpty(config.ClientSecret))
        {
            descriptor.ClientSecret = config.ClientSecret;
        }

        foreach (var redirectUri in config.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri, UriKind.Absolute));
        }

        foreach (var logoutUri in config.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(logoutUri, UriKind.Absolute));
        }

        foreach (var scope in config.Scopes)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        if (string.Equals(config.GrantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
            descriptor.Permissions.Add(Permissions.Endpoints.Revocation);
        }
        else
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
            descriptor.Permissions.Add(Permissions.Endpoints.Revocation);
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
        }

        return descriptor;
    }
}
