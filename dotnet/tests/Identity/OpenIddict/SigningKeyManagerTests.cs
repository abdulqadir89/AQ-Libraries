using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.KeyManagement;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class SigningKeyManagerTests : IDisposable
{
    private readonly TestIdentityDbContext _context;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IDataProtector _protector;
    private readonly ILogger<SigningKeyManager> _logger;
    private readonly KeyManagementOptions _options;
    private readonly SigningKeyManager _manager;

    public SigningKeyManagerTests()
    {
        _context = TestIdentityDbContext.Create();
        _dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        _protector = Substitute.For<IDataProtector>();
        _logger = Substitute.For<ILogger<SigningKeyManager>>();
        _options = new KeyManagementOptions { RotationPeriod = TimeSpan.FromDays(30) };

        _protector.Protect(Arg.Any<byte[]>()).Returns(ci => ci.Arg<byte[]>());
        _dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey").Returns(_protector);

        _manager = new SigningKeyManager(_context, _dataProtectionProvider, _logger, _options);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SeedKey(string keyId, DateTimeOffset expiresAt)
    {
        _context.SigningKeys.Add(new SigningKey(keyId, "encrypted-xml", expiresAt));
        _context.SaveChanges();
    }

    [Fact]
    public void GetActiveSigningKey_WithValidKey_ReturnsKey()
    {
        SeedKey("active-key", DateTimeOffset.UtcNow.AddDays(10));

        var result = _manager.GetActiveSigningKey();

        result.Should().NotBeNull();
        result.KeyId.Should().Be("active-key");
    }

    [Fact]
    public void GetActiveSigningKey_WithNoValidKeys_ThrowsInvalidOperationException()
    {
        var act = () => _manager.GetActiveSigningKey();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No active signing key available.");
    }

    [Fact]
    public async Task GenerateAndPersistKeyAsync_GeneratesNewKey()
    {
        var result = await _manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.KeyId.Should().NotBeEmpty();
        result.EncryptedKeyXml.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(_options.RotationPeriod), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateAndPersistKeyAsync_PersistsKeyToDatabase()
    {
        await _manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        _context.SigningKeys.Should().ContainSingle(k => !string.IsNullOrEmpty(k.KeyId));
    }

    [Fact]
    public async Task GenerateAndPersistKeyAsync_LogsInformationAboutNewKey()
    {
        await _manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Generated new RSA signing key")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task AddAuditEntryAsync_AddsEntryAndSaves()
    {
        var auditEntry = new AuditEntry { Action = "test-action" };

        await _manager.AddAuditEntryAsync(auditEntry, CancellationToken.None);

        _context.AuditLog.Should().ContainSingle(e => e.Action == "test-action");
    }

    [Fact]
    public void RotateNow_GeneratesNewKey()
    {
        _manager.RotateNow();

        _context.SigningKeys.Should().ContainSingle(k => !string.IsNullOrEmpty(k.KeyId));
    }

    [Fact]
    public void GetValidationKeys_WithNoKeys_ReturnsEmptyList()
    {
        var result = _manager.GetValidationKeys();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetValidationKeys_WithValidKey_IncludesKey()
    {
        SeedKey("key1", DateTimeOffset.UtcNow.AddDays(10));

        var result = _manager.GetValidationKeys();

        result.Should().HaveCount(1);
        result.First().KeyId.Should().Be("key1");
    }

    [Fact]
    public void GetValidationKeys_WithRecentlyExpiredKeyWithinOverlap_StillIncludesKey()
    {
        // RotationPeriod=30d in _options; overlap defaults to KeyManagementOptions default (30d).
        // A key that expired 1 hour ago is still within any reasonable overlap window.
        SeedKey("recently-expired", DateTimeOffset.UtcNow.AddHours(-1));

        var result = _manager.GetValidationKeys();

        result.Should().ContainSingle(k => k.KeyId == "recently-expired");
    }

    [Fact]
    public void GetValidationKeys_WithKeyExpiredBeyondOverlap_ExcludesKey()
    {
        var optionsWithShortOverlap = new KeyManagementOptions
        {
            RotationPeriod = TimeSpan.FromDays(30),
            RetirementOverlap = TimeSpan.FromMinutes(5),
        };
        var manager = new SigningKeyManager(_context, _dataProtectionProvider, _logger, optionsWithShortOverlap);
        SeedKey("long-expired", DateTimeOffset.UtcNow.AddHours(-1));

        var result = manager.GetValidationKeys();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RetireExpiredKeysAsync_WithKeyExpiredBeyondOverlap_RetiresIt()
    {
        var optionsWithShortOverlap = new KeyManagementOptions
        {
            RotationPeriod = TimeSpan.FromDays(30),
            RetirementOverlap = TimeSpan.FromMinutes(5),
        };
        var manager = new SigningKeyManager(_context, _dataProtectionProvider, _logger, optionsWithShortOverlap);
        SeedKey("long-expired", DateTimeOffset.UtcNow.AddHours(-1));

        await manager.RetireExpiredKeysAsync(CancellationToken.None);

        var key = _context.SigningKeys.Single(k => k.KeyId == "long-expired");
        key.RetiredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RetireExpiredKeysAsync_WithKeyWithinOverlap_DoesNotRetireIt()
    {
        SeedKey("recently-expired", DateTimeOffset.UtcNow.AddHours(-1));

        await _manager.RetireExpiredKeysAsync(CancellationToken.None);

        var key = _context.SigningKeys.Single(k => k.KeyId == "recently-expired");
        key.RetiredAt.Should().BeNull();
    }

    [Fact]
    public async Task ToSecurityKey_RoundTripsWithRealDataProtectionProvider()
    {
        // Uses a real IDataProtectionProvider (not the mocked no-op one from the fixture)
        // to verify the same provider used to encrypt can decrypt — this is the exact
        // bug that existed when ToSecurityKey() constructed its own EphemeralDataProtectionProvider
        // instead of using the instance-injected one.
        var realProvider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("test-app");
        var manager = new SigningKeyManager(_context, realProvider, _logger, _options);

        var key = await manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        var securityKey = manager.ToSecurityKey(key);

        securityKey.Should().NotBeNull();
        securityKey.KeyId.Should().Be(key.KeyId);
    }

    [Fact]
    public async Task ToSecurityKey_WithDifferentProviderInstance_ThrowsCryptographicException()
    {
        // Regression guard: encrypting with one provider and decrypting with an unrelated
        // ephemeral one (the old bug) must fail loudly, not silently succeed with garbage.
        var encryptingProvider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("app-a");
        var manager = new SigningKeyManager(_context, encryptingProvider, _logger, _options);
        var key = await manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        var unrelatedProvider = new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider();

        var act = () => AQ.Identity.OpenIddict.KeyManagement.SigningKeyExtensions.ToSecurityKey(key, unrelatedProvider);

        act.Should().Throw<System.Security.Cryptography.CryptographicException>();
    }
}
