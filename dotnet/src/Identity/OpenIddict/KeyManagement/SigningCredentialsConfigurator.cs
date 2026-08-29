using System;
using System.Threading;
using AQ.Identity.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace AQ.Identity.OpenIddict.KeyManagement;

/// <summary>
/// Wires OpenIddict's signing/encryption credentials to the persisted, rotating keys
/// managed by <see cref="ISigningKeyManager"/> instead of ephemeral dev certificates.
/// Runs once at options-binding time; generates a bootstrap key if none exists yet.
/// </summary>
public sealed class SigningCredentialsConfigurator : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SigningCredentialsConfigurator> _logger;

    public SigningCredentialsConfigurator(
        IServiceScopeFactory scopeFactory,
        ILogger<SigningCredentialsConfigurator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        using var scope = _scopeFactory.CreateScope();
        var keyManager = scope.ServiceProvider.GetRequiredService<ISigningKeyManager>();

        EnsureActiveKey(keyManager);

        var usableKeys = 0;
        foreach (var validationKey in keyManager.GetValidationKeys())
        {
            // A key can exist in the DB but be undecryptable with the current Data
            // Protection keyring (e.g. the keyring was lost/rotated since the key was
            // written — a fresh container volume, a different environment reusing the
            // same DB). Skip it rather than crashing the whole app on startup.
            SecurityKey securityKey;
            try
            {
                securityKey = keyManager.ToSecurityKey(validationKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Signing key {KeyId} could not be decrypted with the current Data Protection keyring — skipping it.",
                    validationKey.KeyId);
                continue;
            }

            usableKeys++;
            options.SigningCredentials.Add(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256));
            options.EncryptionCredentials.Add(new EncryptingCredentials(
                securityKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512));
        }

        if (usableKeys == 0)
        {
            // Every persisted key was undecryptable (or none existed and the bootstrap
            // key above also failed to decrypt somehow) — generate a fresh one so the
            // server can still start and issue tokens, at the cost of invalidating any
            // tokens signed by the now-unusable keys.
            _logger.LogWarning("No usable signing keys after decryption — generating a new one.");
            var newKey = keyManager.GenerateAndPersistKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
            var securityKey = keyManager.ToSecurityKey(newKey);
            options.SigningCredentials.Add(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256));
            options.EncryptionCredentials.Add(new EncryptingCredentials(
                securityKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512));
        }
    }

    private void EnsureActiveKey(ISigningKeyManager keyManager)
    {
        try
        {
            keyManager.GetActiveKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            _logger.LogInformation("No active signing key found — generating bootstrap key.");
            keyManager.GenerateAndPersistKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
