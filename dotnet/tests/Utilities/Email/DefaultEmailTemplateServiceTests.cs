using System.Globalization;
using AQ.Utilities.Email;
using AQ.Utilities.Email.Resources;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AQ.Utilities.Tests.Email;

public class DefaultEmailTemplateServiceTests
{
    private readonly DefaultEmailTemplateService _service;

    public DefaultEmailTemplateServiceTests()
    {
        // Real ResourceManagerStringLocalizerFactory (not a mock) so localized strings are
        // actually resolved from the .resx files, including the culture fallback chain
        // (zh-HK -> zh-Hant -> neutral).
        var localizerFactory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        var localizer = new StringLocalizer<EmailResource>(localizerFactory);
        _service = new DefaultEmailTemplateService(localizer);
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
        result.HtmlBody.Should().Contain("<html lang="); // lang attribute set to the effective culture (chinese-localization-plan.md Phase 7)
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
        result.HtmlBody.Should().Contain("<html lang="); // lang attribute set to the effective culture (chinese-localization-plan.md Phase 7)
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

    private static readonly CultureInfo ZhTw = CultureInfo.GetCultureInfo("zh-TW");

    [Fact]
    public void BuildVerificationEmail_WithZhTwCulture_HasTraditionalChineseSubject()
    {
        // Arrange
        var toEmail = "user@example.com";
        var verificationUrl = "https://example.com/verify?token=abc123";
        var appName = "Test App";

        // Act
        var result = _service.BuildVerificationEmail(toEmail, verificationUrl, appName, ZhTw);

        // Assert
        result.Subject.Should().Be("驗證你的電子郵件 - Test App");
        result.HtmlBody.Should().Contain("<html lang=\"zh-TW\">");
    }

    [Fact]
    public void BuildPasswordResetEmail_WithZhTwCulture_HasTraditionalChineseSubject()
    {
        // Arrange
        var toEmail = "user@example.com";
        var resetUrl = "https://example.com/reset?token=xyz789";
        var appName = "Test App";

        // Act
        var result = _service.BuildPasswordResetEmail(toEmail, resetUrl, appName, ZhTw);

        // Assert
        result.Subject.Should().Be("重設你的密碼 - Test App");
        result.HtmlBody.Should().Contain("<html lang=\"zh-TW\">");
    }

    [Fact]
    public void BuildWorkspaceInvitationEmail_WithZhTwCulture_HasTraditionalChineseSubject()
    {
        // Arrange
        var toEmail = "user@example.com";
        var acceptUrl = "https://example.com/accept?token=abc123";
        var workspaceName = "Acme Workspace";
        var inviterName = "Jane Doe";
        var appName = "Test App";

        // Act
        var result = _service.BuildWorkspaceInvitationEmail(toEmail, acceptUrl, workspaceName, inviterName, appName, ZhTw);

        // Assert
        result.Subject.Should().Be("你已受邀加入Test App的Acme Workspace");
        result.HtmlBody.Should().Contain("<html lang=\"zh-TW\">");
    }

    [Fact]
    public void BuildSecurityAlertEmail_WithZhTwCulture_HasTraditionalChineseSubject()
    {
        // Arrange
        var toEmail = "user@example.com";
        var eventDescription = "Your password was changed";
        var appName = "Test App";

        // Act
        var result = _service.BuildSecurityAlertEmail(toEmail, eventDescription, appName, ZhTw);

        // Assert
        result.Subject.Should().Be("安全性提醒 - Test App");
        result.HtmlBody.Should().Contain("<html lang=\"zh-TW\">");
    }
}
