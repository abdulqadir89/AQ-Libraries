using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AQ.Identity.OpenIddict.KeyManagement;

public class SigningKeyManager
{
    private readonly IIdentityDbContext _context;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<SigningKeyManager> _logger;
    private readonly KeyManagementOptions _options;
    private SigningKey? _activeKey;
    private IReadOnlyList<SigningKey>? _validationKeys;

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

        InitializeKeys();
    }

    public SigningKey GetActiveSigningKey()
    {
        if (_activeKey == null)
        {
            throw new InvalidOperationException("No active signing key available.");
        }
        return _activeKey;
    }

    public IReadOnlyList<SigningKey> GetValidationKeys()
    {
        return _validationKeys ?? [];
    }

    public void RotateNow()
    {
        GenerateNewSigningKey();
        InitializeKeys();
    }

    private void InitializeKeys()
    {
        var allKeys = _context.SigningKeys
            .Where(k => !k.IsRetired)
            .OrderByDescending(k => k.CreatedAt)
            .ToList();

        if (!allKeys.Any())
        {
            GenerateNewSigningKey();
            allKeys = _context.SigningKeys
                .Where(k => !k.IsRetired)
                .OrderByDescending(k => k.CreatedAt)
                .ToList();
        }

        _activeKey = allKeys.FirstOrDefault(k => !k.IsExpired);
        if (_activeKey == null)
        {
            throw new InvalidOperationException("No non-expired signing key available.");
        }

        var daysUntilExpiry = (_activeKey.ExpiresAt - DateTimeOffset.UtcNow).TotalDays;
        if (daysUntilExpiry <= 30)
        {
            _logger.LogInformation("Active signing key expires in {Days} days. Generating new key.", daysUntilExpiry);
            GenerateNewSigningKey();

            allKeys = _context.SigningKeys
                .Where(k => !k.IsRetired)
                .OrderByDescending(k => k.CreatedAt)
                .ToList();
        }

        _validationKeys = allKeys.Where(k => !k.IsExpired).ToList();
    }

    public void GenerateNewSigningKey()
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
            _context.SaveChangesAsync().GetAwaiter().GetResult();

            var auditEntry = new AuditEntry(
                AuditEntry.Actions.KeyRotated,
                userId: null,
                ip: null,
                ua: null);
            _context.AuditLog.Add(auditEntry);
            _context.SaveChangesAsync().GetAwaiter().GetResult();

            _logger.LogInformation("Generated new RSA signing key {KeyId} expiring at {ExpiresAt}", keyId, expiresAt);
        }
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
