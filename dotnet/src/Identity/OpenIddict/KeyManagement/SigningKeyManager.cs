using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AQ.Identity.OpenIddict.KeyManagement;

public class SigningKeyManager : ISigningKeyManager
{
    private readonly IIdentityDbContext _context;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<SigningKeyManager> _logger;
    private readonly KeyManagementOptions _options;

    public SigningKeyManager(
        IIdentityDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<SigningKeyManager> logger,
        KeyManagementOptions options)
    {
        _context = context;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
        _options = options;
    }

    public async Task<SigningKey> GetActiveKeyAsync(CancellationToken cancellationToken)
    {
        var activeKey = _context.SigningKeys
            .Where(k => !k.IsRetired && !k.IsExpired)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefault();

        if (activeKey == null)
        {
            throw new InvalidOperationException("No active signing key available.");
        }

        return activeKey;
    }

    public async Task<bool> NewerKeyExistsAsync(DateTimeOffset expiresAfter, CancellationToken cancellationToken)
    {
        return _context.SigningKeys
            .Any(k => !k.IsRetired && k.ExpiresAt > expiresAfter);
    }

    public async Task<SigningKey> GenerateAndPersistKeyAsync(CancellationToken cancellationToken)
    {
        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            var keyId = Guid.NewGuid().ToString("N");
            var keyXml = rsa.ToXmlString(includePrivateParameters: true);

            var protector = _dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey");
            var encryptedXml = protector.Protect(keyXml);

            var expiresAt = DateTimeOffset.UtcNow.Add(_options.RotationPeriod);
            var signingKey = new SigningKey(keyId, encryptedXml, expiresAt);

            _context.SigningKeys.Add(signingKey);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generated new RSA signing key {KeyId} expiring at {ExpiresAt}", keyId, expiresAt);
            return signingKey;
        }
    }

    public async Task AddAuditEntryAsync(AuditEntry auditEntry, CancellationToken cancellationToken)
    {
        _context.AuditLog.Add(auditEntry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RetireExpiredKeysAsync(CancellationToken cancellationToken)
    {
        var expiredKeys = _context.SigningKeys
            .Where(k => k.IsExpired && !k.IsRetired)
            .ToList();

        foreach (var key in expiredKeys)
        {
            key.RetiredAt = DateTimeOffset.UtcNow;
        }

        if (expiredKeys.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Retired {Count} expired signing keys", expiredKeys.Count);
        }
    }

    public SigningKey GetActiveSigningKey()
    {
        return GetActiveKeyAsync(CancellationToken.None).Result;
    }

    public IReadOnlyList<SigningKey> GetValidationKeys()
    {
        return _context.SigningKeys
            .Where(k => !k.IsRetired && !k.IsExpired)
            .OrderByDescending(k => k.CreatedAt)
            .ToList();
    }

    public void RotateNow()
    {
        GenerateAndPersistKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private void GenerateNewSigningKey()
    {
        GenerateAndPersistKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}

public static class SigningKeyExtensions
{
    public static SecurityKey ToSecurityKey(this SigningKey key)
    {
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var protector = dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey");
        var decryptedXml = protector.Unprotect(key.EncryptedKeyXml);

        using (var rsa = new RSACryptoServiceProvider())
        {
            rsa.FromXmlString(decryptedXml);
            var rsaParams = rsa.ExportParameters(true);
            return new RsaSecurityKey(new RSAParameters
            {
                D = rsaParams.D,
                DP = rsaParams.DP,
                DQ = rsaParams.DQ,
                Exponent = rsaParams.Exponent,
                InverseQ = rsaParams.InverseQ,
                Modulus = rsaParams.Modulus,
                P = rsaParams.P,
                Q = rsaParams.Q
            })
            {
                KeyId = key.KeyId
            };
        }
    }
}
