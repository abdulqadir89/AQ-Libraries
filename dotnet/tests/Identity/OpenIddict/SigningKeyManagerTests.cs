using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.KeyManagement;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class SigningKeyManagerTests
{
    private readonly IIdentityDbContext _context;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<SigningKeyManager> _logger;
    private readonly KeyManagementOptions _options;
    private readonly SigningKeyManager _manager;

    public SigningKeyManagerTests()
    {
        _context = Substitute.For<IIdentityDbContext>();
        _dataProtectionProvider = Substitute.For<IDataProtectionProvider>();
        _logger = Substitute.For<ILogger<SigningKeyManager>>();
        _options = new KeyManagementOptions { RotationPeriod = TimeSpan.FromDays(30) };
        _manager = new SigningKeyManager(_context, _dataProtectionProvider, _logger, _options);
    }

    [Fact]
    public void GetActiveSigningKey_WithValidKey_ReturnsKey()
    {
        // Arrange
        var validKey = new SigningKey("active-key", "encrypted-xml", DateTimeOffset.UtcNow.AddDays(10));
        var keys = new List<SigningKey> { validKey }.AsQueryable();
        _context.SigningKeys.Returns(keys);

        // Act
        var result = _manager.GetActiveSigningKey();

        // Assert
        result.Should().NotBeNull();
        result.KeyId.Should().Be("active-key");
    }

    [Fact]
    public void GetActiveSigningKey_WithNoValidKeys_ThrowsInvalidOperationException()
    {
        // Arrange
        var keys = new List<SigningKey>().AsQueryable();
        _context.SigningKeys.Returns(keys);

        // Act & Assert
        var act = () => _manager.GetActiveSigningKey();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No active signing key available.");
    }

    [Fact]
    public async Task GenerateAndPersistKeyAsync_GeneratesNewKey()
    {
        // Arrange
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<string>()).Returns("encrypted-xml");
        _dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey").Returns(protector);

        var signingKeysSet = Substitute.For<Microsoft.EntityFrameworkCore.DbSet<SigningKey>>();
        _context.SigningKeys.Returns(signingKeysSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.KeyId.Should().NotBeEmpty();
        result.EncryptedKeyXml.Should().Be("encrypted-xml");
        result.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(_options.RotationPeriod), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateAndPersistKeyAsync_PersistsKeyToDatabase()
    {
        // Arrange
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<string>()).Returns("encrypted-xml");
        _dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey").Returns(protector);

        var signingKeysSet = Substitute.For<Microsoft.EntityFrameworkCore.DbSet<SigningKey>>();
        _context.SigningKeys.Returns(signingKeysSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        // Assert
        signingKeysSet.Received(1).Add(Arg.Is<SigningKey>(k => !string.IsNullOrEmpty(k.KeyId)));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAndPersistKeyAsync_LogsInformationAboutNewKey()
    {
        // Arrange
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<string>()).Returns("encrypted-xml");
        _dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey").Returns(protector);

        var signingKeysSet = Substitute.For<Microsoft.EntityFrameworkCore.DbSet<SigningKey>>();
        _context.SigningKeys.Returns(signingKeysSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _manager.GenerateAndPersistKeyAsync(CancellationToken.None);

        // Assert
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
        // Arrange
        var auditEntry = new AuditEntry { Action = "test-action" };
        var auditLogSet = Substitute.For<Microsoft.EntityFrameworkCore.DbSet<AuditEntry>>();
        _context.AuditLog.Returns(auditLogSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _manager.AddAuditEntryAsync(auditEntry, CancellationToken.None);

        // Assert
        auditLogSet.Received(1).Add(auditEntry);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RotateNow_GeneratesNewKey()
    {
        // Arrange
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<string>()).Returns("encrypted-xml");
        _dataProtectionProvider.CreateProtector("AQ.Identity.SigningKey").Returns(protector);

        var signingKeysSet = Substitute.For<Microsoft.EntityFrameworkCore.DbSet<SigningKey>>();
        _context.SigningKeys.Returns(signingKeysSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        _manager.RotateNow();

        // Assert
        signingKeysSet.Received(1).Add(Arg.Is<SigningKey>(k => !string.IsNullOrEmpty(k.KeyId)));
    }

    [Fact]
    public void GetValidationKeys_WithNoKeys_ReturnsEmptyList()
    {
        // Arrange
        var emptyKeySet = new List<SigningKey>().AsQueryable();
        _context.SigningKeys.Returns(emptyKeySet);

        // Act
        var result = _manager.GetValidationKeys();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetValidationKeys_WithValidKey_IncludesKey()
    {
        // Arrange
        var activeKey = new SigningKey("key1", "encrypted1", DateTimeOffset.UtcNow.AddDays(10));
        var keys = new List<SigningKey> { activeKey }.AsQueryable();
        _context.SigningKeys.Returns(keys);

        // Act
        var result = _manager.GetValidationKeys();

        // Assert
        result.Should().HaveCount(1);
        result.First().KeyId.Should().Be("key1");
    }
}
