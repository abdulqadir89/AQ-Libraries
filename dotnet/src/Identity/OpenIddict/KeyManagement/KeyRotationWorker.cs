using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AQ.Identity.OpenIddict.KeyManagement;

public sealed class KeyRotationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeyRotationWorker> _logger;
    private readonly KeyManagementOptions _options;
    private readonly TimeSpan _rotationInterval = TimeSpan.FromHours(24);

    public KeyRotationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<KeyRotationWorker> logger,
        KeyManagementOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var keyManager = scope.ServiceProvider.GetRequiredService<ISigningKeyManager>();
                await RotateKeysIfNeededAsync(keyManager, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Key rotation failed");
            }

            await Task.Delay(_rotationInterval, stoppingToken);
        }
    }

    private async Task RotateKeysIfNeededAsync(ISigningKeyManager keyManager, CancellationToken cancellationToken)
    {
        SigningKey activeKey;
        try
        {
            activeKey = await keyManager.GetActiveKeyAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // No key exists yet — normally SigningCredentialsConfigurator bootstraps one
            // before the server starts accepting requests, but this worker's first tick
            // can race ahead of that. Self-heal rather than treating it as an error.
            _logger.LogInformation("No active signing key found — generating bootstrap key.");
            await keyManager.GenerateAndPersistKeyAsync(cancellationToken);
            return;
        }

        if (activeKey.ExpiresAt < DateTimeOffset.UtcNow.Add(_options.RetirementOverlap))
        {
            var newerKeyExists = await keyManager.NewerKeyExistsAsync(
                activeKey.ExpiresAt,
                cancellationToken);

            if (!newerKeyExists)
            {
                var newKey = await keyManager.GenerateAndPersistKeyAsync(cancellationToken);
                var auditEntry = new AuditEntry(
                    AuditEntry.Actions.KeyRotated,
                    userId: null,
                    ip: null,
                    ua: null);
                await keyManager.AddAuditEntryAsync(
                    auditEntry,
                    cancellationToken);

                _logger.LogInformation("Rotated signing key {KeyId}", newKey.Id);
            }
        }

        await keyManager.RetireExpiredKeysAsync(cancellationToken);
    }
}
