using System.Collections.Immutable;
using AQ.Identity.Core.Configuration;
using AQ.Identity.OpenIddict.Management.Endpoints.Clients;
using FluentAssertions;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class ClientDescriptorBuilderTests
{
    [Theory]
    [InlineData("https://example.com/callback")]
    [InlineData("http://localhost:3000/callback")]
    [InlineData("http://127.0.0.1:3000/callback")]
    [InlineData("http://[::1]:3000/callback")]
    [InlineData("http://host.docker.internal:3000/callback")]
    [InlineData("msauth.com.els.mobile://auth")]
    public void ValidateRedirectUri_WithAllowedUri_ReturnsNull(string uri)
    {
        ClientDescriptorBuilder.ValidateRedirectUri(uri).Should().BeNull();
    }

    [Theory]
    [InlineData("http://example.com/callback")]
    [InlineData("http://evil.com/callback")]
    [InlineData("not-a-uri")]
    public void ValidateRedirectUri_WithDisallowedUri_ReturnsError(string uri)
    {
        ClientDescriptorBuilder.ValidateRedirectUri(uri).Should().NotBeNull();
    }

    [Fact]
    public void ValidateRedirectUris_WithFirstUriInvalid_ReturnsThatError()
    {
        var error = ClientDescriptorBuilder.ValidateRedirectUris(
            ["http://evil.com/callback", "https://example.com/callback"]);

        error.Should().Contain("evil.com");
    }

    [Fact]
    public void ValidateRedirectUris_WithAllValid_ReturnsNull()
    {
        var error = ClientDescriptorBuilder.ValidateRedirectUris(
            ["https://example.com/callback", "http://localhost:3000/callback"]);

        error.Should().BeNull();
    }

    [Fact]
    public void Build_WithInvalidRedirectUri_ThrowsArgumentException()
    {
        var config = new IdentityClientConfig
        {
            ClientId = "bad-client",
            DisplayName = "Bad Client",
            RedirectUris = ["http://evil.com/callback"],
        };

        var act = () => ClientDescriptorBuilder.Build(config);

        act.Should().Throw<ArgumentException>().WithMessage("*bad-client*");
    }

    [Fact]
    public void Build_WithInvalidPostLogoutRedirectUri_ThrowsArgumentException()
    {
        var config = new IdentityClientConfig
        {
            ClientId = "bad-client",
            DisplayName = "Bad Client",
            RedirectUris = ["https://example.com/callback"],
            PostLogoutRedirectUris = ["http://evil.com/logout"],
        };

        var act = () => ClientDescriptorBuilder.Build(config);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_WithValidUris_Succeeds()
    {
        var config = new IdentityClientConfig
        {
            ClientId = "good-client",
            DisplayName = "Good Client",
            RedirectUris = ["https://example.com/callback"],
            PostLogoutRedirectUris = ["https://example.com/logout"],
        };

        var descriptor = ClientDescriptorBuilder.Build(config);

        descriptor.ClientId.Should().Be("good-client");
    }

    [Theory]
    [InlineData("https://web.example.com/api/auth/backchannel-logout")]
    [InlineData("http://host.docker.internal:3000/api/auth/backchannel-logout")]
    public void ValidateBackchannelLogoutUri_WithAllowedUri_ReturnsNull(string uri)
    {
        ClientDescriptorBuilder.ValidateBackchannelLogoutUri(uri).Should().BeNull();
    }

    [Theory]
    [InlineData("http://web.example.com/logout")]
    [InlineData("https://web.example.com/logout#frag")]
    [InlineData("msauth.com.els.mobile://logout")]
    [InlineData("/relative/logout")]
    public void ValidateBackchannelLogoutUri_WithDisallowedUri_ReturnsError(string uri)
    {
        ClientDescriptorBuilder.ValidateBackchannelLogoutUri(uri).Should().NotBeNull();
    }

    [Fact]
    public void Build_WithBackchannelLogoutUri_StoresTheClientMetadata()
    {
        var descriptor = ClientDescriptorBuilder.Build(new IdentityClientConfig
        {
            ClientId = "web",
            DisplayName = "Web",
            BackchannelLogoutUri = "https://web.example.com/api/auth/backchannel-logout",
            BackchannelLogoutSessionRequired = true,
        });

        var (uri, sessionRequired) = AQ.Identity.OpenIddict.Sessions.BackchannelLogout.ReadClientSettings(descriptor.Properties.ToImmutableDictionary());
        uri.Should().Be("https://web.example.com/api/auth/backchannel-logout");
        sessionRequired.Should().BeTrue();
    }
}
