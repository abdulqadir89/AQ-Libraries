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

        _protector.Protect(Arg.Any<string>()).Returns("encrypted-xml");
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
        result.EncryptedKeyXml.Should().Be("encrypted-xml");
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
}
