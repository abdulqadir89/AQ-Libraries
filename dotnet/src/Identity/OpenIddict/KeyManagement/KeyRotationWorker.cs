using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.KeyManagement;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AQ.Identity.OpenIddict.KeyManagement;

public sealed class KeyRotationWorker : BackgroundService
{
    private readonly ISigningKeyManager _keyManager;
    private readonly ILogger<KeyRotationWorker> _logger;
    private readonly TimeSpan _rotationInterval = TimeSpan.FromHours(24);
    private readonly TimeSpan _expirationThreshold = TimeSpan.FromDays(30);

    public KeyRotationWorker(
        ISigningKeyManager keyManager,
        ILogger<KeyRotationWorker> logger)
    {
        _keyManager = keyManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RotateKeysIfNeededAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Key rotation failed");
            }

            await Task.Delay(_rotationInterval, stoppingToken);
        }
    }

    private async Task RotateKeysIfNeededAsync(CancellationToken cancellationToken)
    {
        var activeKey = await _keyManager.GetActiveKeyAsync(cancellationToken);

        if (activeKey.ExpiresAt < DateTimeOffset.UtcNow.Add(_expirationThreshold))
        {
            var newerKeyExists = await _keyManager.NewerKeyExistsAsync(
                activeKey.ExpiresAt,
                cancellationToken);

            if (!newerKeyExists)
            {
                var newKey = await _keyManager.GenerateAndPersistKeyAsync(cancellationToken);
                var auditEntry = new AuditEntry(
                    AuditEntry.Actions.KeyRotated,
                    userId: null,
                    ip: null,
                    ua: null);
                await _keyManager.AddAuditEntryAsync(
                    auditEntry,
                    cancellationToken);

                _logger.LogInformation("Rotated signing key {KeyId}", newKey.Id);
            }
        }

        await _keyManager.RetireExpiredKeysAsync(cancellationToken);
    }
}