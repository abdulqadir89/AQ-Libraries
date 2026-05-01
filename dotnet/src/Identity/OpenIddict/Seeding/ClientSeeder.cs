using AQ.Identity.Core.Configuration;
using AQ.Identity.OpenIddict.Management.Endpoints.Clients;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Seeding;

public class ClientSeeder : IHostedService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<ClientSeeder> _logger;
    private readonly IReadOnlyList<IdentityClientConfig> _clients;

    public ClientSeeder(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        ILogger<ClientSeeder> logger,
        IReadOnlyList<IdentityClientConfig> clients)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _logger = logger;
        _clients = clients;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var standardScopes = new[] { Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess };
        var customScopes = _clients
            .SelectMany(c => c.Scopes)
            .Distinct()
            .Except(standardScopes)
            .ToList();

        foreach (var scope in standardScopes.Concat(customScopes))
        {
            var existingScope = await _scopeManager.FindByNameAsync(scope, cancellationToken);
            if (existingScope == null)
            {
                var scopeDescriptor = new OpenIddictScopeDescriptor
                {
                    Name = scope,
                    DisplayName = scope
                };
                await _scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
                _logger.LogInformation("Created scope: {Scope}", scope);
            }
        }

        foreach (var clientConfig in _clients)
        {
            var existingClient = await _applicationManager.FindByClientIdAsync(clientConfig.ClientId, cancellationToken);

            if (existingClient != null)
            {
                await UpdateClientAsync(existingClient, clientConfig, cancellationToken);
                _logger.LogInformation("Updated OpenIddict client: {ClientId}", clientConfig.ClientId);
            }
            else
            {
                await CreateClientAsync(clientConfig, cancellationToken);
                _logger.LogInformation("Created OpenIddict client: {ClientId}", clientConfig.ClientId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateClientAsync(IdentityClientConfig config, CancellationToken cancellationToken)
    {
        var descriptor = ClientDescriptorBuilder.Build(config);
        await _applicationManager.CreateAsync(descriptor, cancellationToken);
    }

    private async Task UpdateClientAsync(object existingClient, IdentityClientConfig config, CancellationToken cancellationToken)
    {
        var descriptor = ClientDescriptorBuilder.Build(config);
        await _applicationManager.UpdateAsync(existingClient, descriptor, cancellationToken);
    }
}
