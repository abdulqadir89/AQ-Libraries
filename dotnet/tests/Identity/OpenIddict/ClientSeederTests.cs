using AQ.Identity.Core.Configuration;
using AQ.Identity.OpenIddict.Seeding;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Tests;

public class ClientSeederTests
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<ClientSeeder> _logger;

    public ClientSeederTests()
    {
        _applicationManager = Substitute.For<IOpenIddictApplicationManager>();
        _scopeManager = Substitute.For<IOpenIddictScopeManager>();
        _logger = Substitute.For<ILogger<ClientSeeder>>();
    }

    private ClientSeeder CreateSeeder(IReadOnlyList<IdentityClientConfig> clients)
    {
        return new ClientSeeder(_applicationManager, _scopeManager, _logger, clients);
    }

    [Fact]
    public async Task StartAsync_RunningTwice_DoesNotDuplicateClients()
    {
        // Arrange
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "test-client",
            DisplayName = "Test Client",
            RedirectUris = ["http://localhost:3000/callback"],
            PostLogoutRedirectUris = ["http://localhost:3000/logout"],
            Scopes = ["openid", "profile", "email"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        // Setup: First run creates the client
        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync("test-client", CancellationToken.None).Returns((object?)null);

        // Act - First run
        await seeder.StartAsync(CancellationToken.None);

        // Assert - First run created the client
        await _applicationManager.Received(1).CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), CancellationToken.None);
        _applicationManager.ClearReceivedCalls();

        // Setup: Second run finds existing client
        var existingClient = new object();
        _applicationManager.FindByClientIdAsync("test-client", CancellationToken.None).Returns(existingClient);

        // Act - Second run
        await seeder.StartAsync(CancellationToken.None);

        // Assert - Second run updated instead of creating
        await _applicationManager.DidNotReceive().CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), CancellationToken.None);
        await _applicationManager.Received(1).UpdateAsync(existingClient, Arg.Any<OpenIddictApplicationDescriptor>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithEmptyClientList_OnlyCreatesStandardScopes()
    {
        // Arrange
        var clients = Array.Empty<IdentityClientConfig>();
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        var standardScopes = new[] { Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess };
        foreach (var scope in standardScopes)
        {
            await _scopeManager.Received(1).CreateAsync(Arg.Is<OpenIddictScopeDescriptor>(d => d.Name == scope), CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WithCustomScopes_CreatesStandardAndCustomScopes()
    {
        // Arrange
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "test-client",
            DisplayName = "Test Client",
            RedirectUris = ["http://localhost:3000/callback"],
            PostLogoutRedirectUris = ["http://localhost:3000/logout"],
            Scopes = ["openid", "profile", "custom:api", "custom:admin"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert - Verify custom scopes were created
        var standardScopes = new[] { Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess };
        var allExpectedScopes = standardScopes.Concat(new[] { "custom:api", "custom:admin" }).Distinct();

        foreach (var scope in allExpectedScopes)
        {
            await _scopeManager.Received(1).CreateAsync(Arg.Is<OpenIddictScopeDescriptor>(d => d.Name == scope), CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WithExistingScope_DoesNotRecreateScope()
    {
        // Arrange
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "test-client",
            DisplayName = "Test Client",
            RedirectUris = ["http://localhost:3000/callback"],
            PostLogoutRedirectUris = ["http://localhost:3000/logout"],
            Scopes = ["openid", "profile"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        // Setup: openid scope exists, others don't
        _scopeManager.FindByNameAsync(Scopes.OpenId, CancellationToken.None).Returns(new object());
        _scopeManager.FindByNameAsync(Arg.Is<string>(s => s != Scopes.OpenId), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert - openid scope should not be created, others should
        await _scopeManager.DidNotReceive().CreateAsync(Arg.Is<OpenIddictScopeDescriptor>(d => d.Name == Scopes.OpenId), CancellationToken.None);
        await _scopeManager.Received(1).CreateAsync(Arg.Is<OpenIddictScopeDescriptor>(d => d.Name == Scopes.Profile), CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithDuplicateCustomScopes_DeduplicatesBeforeCreating()
    {
        // Arrange
        var client1 = new IdentityClientConfig
        {
            ClientId = "client1",
            DisplayName = "Client 1",
            RedirectUris = ["http://localhost:3000/callback"],
            PostLogoutRedirectUris = ["http://localhost:3000/logout"],
            Scopes = ["openid", "custom:api"]
        };

        var client2 = new IdentityClientConfig
        {
            ClientId = "client2",
            DisplayName = "Client 2",
            RedirectUris = ["http://localhost:3001/callback"],
            PostLogoutRedirectUris = ["http://localhost:3001/logout"],
            Scopes = ["openid", "custom:api"]
        };

        var clients = new[] { client1, client2 };
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert - custom:api should only be created once
        await _scopeManager.Received(1).CreateAsync(Arg.Is<OpenIddictScopeDescriptor>(d => d.Name == "custom:api"), CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_CreatesNewClient_WithCorrectDescriptor()
    {
        // Arrange
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "new-client",
            DisplayName = "New Client",
            RedirectUris = ["http://localhost:4000/callback"],
            PostLogoutRedirectUris = ["http://localhost:4000/logout"],
            Scopes = ["openid", "profile"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync("new-client", CancellationToken.None).Returns((object?)null);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        await _applicationManager.Received(1).CreateAsync(Arg.Is<OpenIddictApplicationDescriptor>(d => d.ClientId == "new-client"), CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_UpdatesExistingClient_WithCorrectDescriptor()
    {
        // Arrange
        var existingClient = new object();
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "existing-client",
            DisplayName = "Existing Client",
            RedirectUris = ["http://localhost:5000/callback"],
            PostLogoutRedirectUris = ["http://localhost:5000/logout"],
            Scopes = ["openid"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync("existing-client", CancellationToken.None).Returns(existingClient);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        await _applicationManager.Received(1).UpdateAsync(
            existingClient,
            Arg.Is<OpenIddictApplicationDescriptor>(d => d.ClientId == "existing-client"),
            CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var seeder = CreateSeeder(Array.Empty<IdentityClientConfig>());

        // Act - should not throw
        await seeder.StopAsync(CancellationToken.None);

        // Assert - if we got here, it succeeded
        true.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_LogsInformationForCreatedClient()
    {
        // Arrange
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "logged-client",
            DisplayName = "Logged Client",
            RedirectUris = ["http://localhost:3000/callback"],
            PostLogoutRedirectUris = ["http://localhost:3000/logout"],
            Scopes = ["openid"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync("logged-client", CancellationToken.None).Returns((object?)null);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Created OpenIddict client")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task StartAsync_LogsInformationForUpdatedClient()
    {
        // Arrange
        var existingClient = new object();
        var clientConfig = new IdentityClientConfig
        {
            ClientId = "updated-client",
            DisplayName = "Updated Client",
            RedirectUris = ["http://localhost:3000/callback"],
            PostLogoutRedirectUris = ["http://localhost:3000/logout"],
            Scopes = ["openid"]
        };

        var clients = new[] { clientConfig };
        var seeder = CreateSeeder(clients);

        _scopeManager.FindByNameAsync(Arg.Any<string>(), CancellationToken.None).Returns((object?)null);
        _applicationManager.FindByClientIdAsync("updated-client", CancellationToken.None).Returns(existingClient);

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Updated OpenIddict client")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
