using AQ.Identity.Email;
using FluentAssertions;
using Xunit;

namespace AQ.Identity.Core.Tests;

public class DefaultEmailTemplateServiceTests
{
    private readonly DefaultEmailTemplateService _service;

    public DefaultEmailTemplateServiceTests()
    {
        _service = new DefaultEmailTemplateService();
    }

    [Fact]
    public void BuildVerificationEmail_IncludesVerificationUrlInHtmlBody()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify?token=abc123";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain(verificationUrl);
        result.HtmlBody.Should().Contain("<a href=");
    }

    [Fact]
    public void BuildVerificationEmail_IncludesVerificationUrlInTextBody()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify?token=abc123";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.TextBody.Should().Contain(verificationUrl);
    }

    [Fact]
    public void BuildVerificationEmail_HasCorrectSubject()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify?token=abc123";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.Subject.Should().Contain("Verify your email");
        result.Subject.Should().Contain(appName);
    }

    [Fact]
    public void BuildVerificationEmail_HasCorrectRecipient()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify?token=abc123";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.To.Should().Be(toEmail);
    }

    [Fact]
    public void BuildVerificationEmail_IncludesAppNameInBothBodies()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify";
        var appName = "My Custom App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain(appName);
        result.TextBody.Should().Contain(appName);
    }

    [Fact]
    public void BuildVerificationEmail_HtmlBodyIsValidHtml()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain("<!DOCTYPE html>");
        result.HtmlBody.Should().Contain("<html>");
        result.HtmlBody.Should().Contain("</html>");
        result.HtmlBody.Should().Contain("<body");
        result.HtmlBody.Should().Contain("</body>");
    }

    [Fact]
    public void BuildVerificationEmail_TextBodyDoesNotContainHtml()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.TextBody.Should().NotContain("<html");
        result.TextBody.Should().NotContain("</html>");
        result.TextBody.Should().NotContain("<body");
    }

    [Fact]
    public void BuildVerificationEmail_IncludesVerificationButtonInHtml()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain("Verify Email");
    }

    [Fact]
    public void BuildVerificationEmail_TextBodyIncludesInstructions()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.TextBody.Should().Contain("Please verify your email");
    }

    [Fact]
    public void BuildVerificationEmail_WithSpecialCharactersInUrl_HandlesCorrectly()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify?token=abc123&user=test@example.com";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain(verificationUrl);
        result.TextBody.Should().Contain(verificationUrl);
    }

    [Fact]
    public void BuildPasswordResetEmail_IncludesResetUrlInHtmlBody()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset?token=xyz789";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain(resetUrl);
        result.HtmlBody.Should().Contain("<a href=");
    }

    [Fact]
    public void BuildPasswordResetEmail_IncludesResetUrlInTextBody()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset?token=xyz789";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.TextBody.Should().Contain(resetUrl);
    }

    [Fact]
    public void BuildPasswordResetEmail_HasCorrectSubject()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset?token=xyz789";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.Subject.Should().Contain("Reset your password");
        result.Subject.Should().Contain(appName);
    }

    [Fact]
    public void BuildPasswordResetEmail_HasCorrectRecipient()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset?token=xyz789";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.To.Should().Be(toEmail);
    }

    [Fact]
    public void BuildPasswordResetEmail_IncludesAppNameInBothBodies()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset";
        var appName = "My Custom App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain(appName);
        result.TextBody.Should().Contain(appName);
    }

    [Fact]
    public void BuildPasswordResetEmail_HtmlBodyIsValidHtml()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain("<!DOCTYPE html>");
        result.HtmlBody.Should().Contain("<html>");
        result.HtmlBody.Should().Contain("</html>");
        result.HtmlBody.Should().Contain("<body");
        result.HtmlBody.Should().Contain("</body>");
    }

    [Fact]
    public void BuildPasswordResetEmail_TextBodyDoesNotContainHtml()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.TextBody.Should().NotContain("<html");
        result.TextBody.Should().NotContain("</html>");
        result.TextBody.Should().NotContain("<body");
    }

    [Fact]
    public void BuildPasswordResetEmail_IncludesResetButtonInHtml()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain("Reset Password");
    }

    [Fact]
    public void BuildPasswordResetEmail_TextBodyIncludesInstructions()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.TextBody.Should().Contain("Please visit the following link");
    }

    [Fact]
    public void BuildPasswordResetEmail_WithSpecialCharactersInUrl_HandlesCorrectly()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset?token=xyz789&user=test@example.com";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.HtmlBody.Should().Contain(resetUrl);
        result.TextBody.Should().Contain(resetUrl);
    }

    [Fact]
    public void BuildVerificationEmail_SubjectDoesNotContainLineBreaks()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName);

        // Assert
        result.Subject.Should().NotContain("\n");
        result.Subject.Should().NotContain("\r");
    }

    [Fact]
    public void BuildPasswordResetEmail_SubjectDoesNotContainLineBreaks()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName);

        // Assert
        result.Subject.Should().NotContain("\n");
        result.Subject.Should().NotContain("\r");
    }
}
