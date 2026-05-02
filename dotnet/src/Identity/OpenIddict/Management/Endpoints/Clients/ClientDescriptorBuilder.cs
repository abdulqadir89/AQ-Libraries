using AQ.Identity.Core.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Clients;

internal static class ClientDescriptorBuilder
{
    internal static OpenIddictApplicationDescriptor Build(IdentityClientConfig config)
    {
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

        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
        descriptor.Permissions.Add(Permissions.Endpoints.Revocation);

        return descriptor;
    }
}
