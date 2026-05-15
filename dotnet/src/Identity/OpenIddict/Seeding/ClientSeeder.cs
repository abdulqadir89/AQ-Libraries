using AQ.Identity.Core.Configuration;
using AQ.Identity.OpenIddict.Management.Endpoints.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Seeding;

public class ClientSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClientSeeder> _logger;
    private readonly IReadOnlyList<IdentityClientConfig> _clients;

    public ClientSeeder(
        IServiceProvider serviceProvider,
        ILogger<ClientSeeder> logger,
        IReadOnlyList<IdentityClientConfig> clients)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _clients = clients;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var diScope = _serviceProvider.CreateScope();
        var applicationManager = diScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = diScope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        var standardScopes = new[] { Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess };
        var customScopes = _clients
            .SelectMany(c => c.Scopes)
            .Distinct()
            .Except(standardScopes)
            .ToList();

        foreach (var scope in standardScopes.Concat(customScopes))
        {
            var existingScope = await scopeManager.FindByNameAsync(scope, cancellationToken);
            if (existingScope == null)
            {
                var scopeDescriptor = new OpenIddictScopeDescriptor
                {
                    Name = scope,
                    DisplayName = scope
                };
                await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
                _logger.LogInformation("Created scope: {Scope}", scope);
            }
        }

        foreach (var clientConfig in _clients)
        {
            if (string.IsNullOrEmpty(clientConfig?.ClientId))
            {
                _logger.LogWarning("Skipping client with missing ClientId");
                continue;
            }

            var existingClient = await applicationManager.FindByClientIdAsync(clientConfig.ClientId, cancellationToken);

            if (existingClient != null)
            {
                await UpdateClientAsync(existingClient, clientConfig, applicationManager, cancellationToken);
                _logger.LogInformation("Updated OpenIddict client: {ClientId}", clientConfig.ClientId);
            }
            else
            {
                await CreateClientAsync(clientConfig, applicationManager, cancellationToken);
                _logger.LogInformation("Created OpenIddict client: {ClientId}", clientConfig.ClientId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateClientAsync(IdentityClientConfig config, IOpenIddictApplicationManager applicationManager, CancellationToken cancellationToken)
    {
        var descriptor = ClientDescriptorBuilder.Build(config);
        await applicationManager.CreateAsync(descriptor, cancellationToken);
    }

    private async Task UpdateClientAsync(object existingClient, IdentityClientConfig config, IOpenIddictApplicationManager applicationManager, CancellationToken cancellationToken)
    {
        var descriptor = ClientDescriptorBuilder.Build(config);
        await applicationManager.UpdateAsync(existingClient, descriptor, cancellationToken);
    }
}
