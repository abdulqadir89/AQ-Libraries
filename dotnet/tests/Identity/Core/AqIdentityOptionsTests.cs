using AQ.Identity.Core.Configuration;
using FluentAssertions;
using Xunit;

namespace AQ.Identity.Core.Tests;

public class AqIdentityOptionsTests
{
    [Fact]
    public void DefaultAppName_IsCorrect()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.AppName.Should().Be("AQ Identity");
    }

    [Fact]
    public void LockoutDuration_DefaultIsCorrect()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Lockout.LockoutDuration.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void MaxFailedAttempts_DefaultIsCorrect()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Lockout.MaxFailedAttempts.Should().Be(5);
    }

    [Fact]
    public void AccessTokenLifetime_DefaultIsCorrect()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Tokens.AccessToken.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void RefreshTokenLifetime_DefaultIsCorrect()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Tokens.RefreshToken.Should().Be(TimeSpan.FromDays(14));
    }

    [Fact]
    public void TokensNestedObject_IsInitialized()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Tokens.Should().NotBeNull();
    }

    [Fact]
    public void PasswordNestedObject_IsInitialized()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Password.Should().NotBeNull();
    }

    [Fact]
    public void LockoutNestedObject_IsInitialized()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Lockout.Should().NotBeNull();
    }

    [Fact]
    public void KeysNestedObject_IsInitialized()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Keys.Should().NotBeNull();
    }

    [Fact]
    public void EmailNestedObject_IsInitialized()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Email.Should().NotBeNull();
    }

    [Fact]
    public void GoogleOptions_IsNullByDefault()
    {
        // Arrange & Act
        var options = new AqIdentityOptions();

        // Assert
        options.Google.Should().BeNull();
    }

    [Fact]
    public void AppNameCanBeChanged()
    {
        // Arrange
        var options = new AqIdentityOptions();
        var newAppName = "My Custom App";

        // Act
        options.AppName = newAppName;

        // Assert
        options.AppName.Should().Be(newAppName);
    }

    [Fact]
    public void LockoutDurationCanBeChanged()
    {
        // Arrange
        var options = new AqIdentityOptions();
        var newDuration = TimeSpan.FromMinutes(30);

        // Act
        options.Lockout.LockoutDuration = newDuration;

        // Assert
        options.Lockout.LockoutDuration.Should().Be(newDuration);
    }

    [Fact]
    public void AccessTokenLifetimeCanBeChanged()
    {
        // Arrange
        var options = new AqIdentityOptions();
        var newLifetime = TimeSpan.FromHours(2);

        // Act
        options.Tokens.AccessToken = newLifetime;

        // Assert
        options.Tokens.AccessToken.Should().Be(newLifetime);
    }

    [Fact]
    public void RefreshTokenLifetimeCanBeChanged()
    {
        // Arrange
        var options = new AqIdentityOptions();
        var newLifetime = TimeSpan.FromDays(30);

        // Act
        options.Tokens.RefreshToken = newLifetime;

        // Assert
        options.Tokens.RefreshToken.Should().Be(newLifetime);
    }

    [Fact]
    public void MaxFailedAttemptsCanBeChanged()
    {
        // Arrange
        var options = new AqIdentityOptions();
        var newLimit = 10;

        // Act
        options.Lockout.MaxFailedAttempts = newLimit;

        // Assert
        options.Lockout.MaxFailedAttempts.Should().Be(newLimit);
    }

    [Fact]
    public void IssuerCanBeSet()
    {
        // Arrange
        var options = new AqIdentityOptions();
        var issuer = "https://auth.example.com";

        // Act
        options.Issuer = issuer;

        // Assert
        options.Issuer.Should().Be(issuer);
    }

    [Fact]
    public void MultipleInstancesHaveIndependentDefaults()
    {
        // Arrange & Act
        var options1 = new AqIdentityOptions { AppName = "App1" };
        var options2 = new AqIdentityOptions { AppName = "App2" };

        // Assert
        options1.AppName.Should().Be("App1");
        options2.AppName.Should().Be("App2");
    }

    [Fact]
    public void AllDefaultsAreConsistentAcrossInstances()
    {
        // Arrange & Act
        var options1 = new AqIdentityOptions();
        var options2 = new AqIdentityOptions();

        // Assert
        options1.Lockout.LockoutDuration.Should().Be(options2.Lockout.LockoutDuration);
        options1.Tokens.AccessToken.Should().Be(options2.Tokens.AccessToken);
        options1.Tokens.RefreshToken.Should().Be(options2.Tokens.RefreshToken);
        options1.Lockout.MaxFailedAttempts.Should().Be(options2.Lockout.MaxFailedAttempts);
    }
}
