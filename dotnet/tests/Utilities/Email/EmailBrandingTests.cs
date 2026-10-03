using AQ.Utilities.Email;
using AQ.Utilities.Email.Resources;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AQ.Utilities.Tests.Email;

public class EmailBrandingTests
{
    private sealed class StaticMonitor(EmailBrandingOptions value) : IOptionsMonitor<EmailBrandingOptions>
    {
        public EmailBrandingOptions CurrentValue => value;
        public EmailBrandingOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<EmailBrandingOptions, string?> listener) => null;
    }

    private static readonly EmailBrandingOptions Brand = new()
    {
        LogoUrl = "https://example.com/logo.png",
        PrimaryColor = "#C93A28",
        BackgroundColor = "#f5efe6",
        Tagline = "Learn well",
        WebsiteUrl = "https://example.com",
        SupportEmail = "help@example.com",
        FooterText = "Example Ltd"
    };

    private static IStringLocalizer<EmailResource> Localizer() =>
        new StringLocalizer<EmailResource>(new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance));

    private static DefaultEmailTemplateService Branded() => new(Localizer(), new StaticMonitor(Brand));

    private static DefaultEmailTemplateService Plain() => new(Localizer());

    public static IEnumerable<object[]> AllEmails()
    {
        yield return new object[] { (Func<DefaultEmailTemplateService, EmailMessage>)(s => s.BuildVerificationEmail("a@b.com", "https://x.test/v?a=1&b=2", "App")) };
        yield return new object[] { (Func<DefaultEmailTemplateService, EmailMessage>)(s => s.BuildPasswordResetEmail("a@b.com", "https://x.test/r?a=1&b=2", "App")) };
        yield return new object[] { (Func<DefaultEmailTemplateService, EmailMessage>)(s => s.BuildWorkspaceInvitationEmail("a@b.com", "https://x.test/i", "WS", "Inviter", "App")) };
        yield return new object[] { (Func<DefaultEmailTemplateService, EmailMessage>)(s => s.BuildSecurityAlertEmail("a@b.com", "Your password was changed", "App")) };
    }

    [Theory]
    [MemberData(nameof(AllEmails))]
    public void CustomBranding_IsApplied(Func<DefaultEmailTemplateService, EmailMessage> build)
    {
        var message = build(Branded());

        message.HtmlBody.Should().Contain("<img src=\"https://example.com/logo.png\"");
        message.HtmlBody.Should().Contain("#C93A28");
        message.HtmlBody.Should().Contain("#f5efe6");
        message.HtmlBody.Should().Contain("Learn well");
        message.HtmlBody.Should().Contain("mailto:help@example.com");
        message.HtmlBody.Should().Contain("Example Ltd");
        message.TextBody.Should().Contain("help@example.com");
        message.TextBody.Should().Contain("Example Ltd");
    }

    [Theory]
    [MemberData(nameof(AllEmails))]
    public void DefaultBranding_IsNeutral(Func<DefaultEmailTemplateService, EmailMessage> build)
    {
        var message = build(Plain());

        message.HtmlBody.Should().NotContain("<img");
        message.HtmlBody.Should().NotContain("mailto:");
        message.HtmlBody.Should().Contain("#007bff");
        message.HtmlBody.Should().Contain("App");
    }

    [Fact]
    public void ActionEmails_HaveButtonAndFallbackLink_SecurityAlertHasNone()
    {
        var service = Branded();

        var reset = service.BuildPasswordResetEmail("a@b.com", "https://x.test/r", "App");
        var invite = service.BuildWorkspaceInvitationEmail("a@b.com", "https://x.test/i", "WS", "Inviter", "App");
        var alert = service.BuildSecurityAlertEmail("a@b.com", "Your password was changed", "App");

        reset.HtmlBody.Should().Contain("href=\"https://x.test/r\"");
        invite.HtmlBody.Should().Contain("href=\"https://x.test/i\"");
        alert.HtmlBody.Should().NotContain("<table role=\"presentation\" style=\"margin: 28px 0");
        alert.HtmlBody.Should().Contain("Your password was changed");
    }

    [Fact]
    public void Branding_EncodesUntrustedText()
    {
        var service = new DefaultEmailTemplateService(Localizer(), new StaticMonitor(new EmailBrandingOptions
        {
            Tagline = "<script>alert(1)</script>",
            LogoUrl = "https://x.test/l.png\" onerror=\"x"
        }));

        var message = service.BuildVerificationEmail("a@b.com", "https://x.test/v", "App");

        message.HtmlBody.Should().NotContain("<script>");
        message.HtmlBody.Should().NotContain("\" onerror=\"");
    }

    [Fact]
    public void FooterReason_CustomValueReplacesDefault_AndFormatsAppName()
    {
        var service = new DefaultEmailTemplateService(Localizer(), new StaticMonitor(new EmailBrandingOptions
        {
            FooterReason = "Sent by {0} <system>"
        }));

        var message = service.BuildPasswordResetEmail("a@b.com", "https://x.test/r", "My App");

        message.HtmlBody.Should().Contain("Sent by My App &lt;system&gt;");
        message.TextBody.Should().Contain("Sent by My App <system>");
        message.HtmlBody.Should().NotContain("because of a request");
    }

    [Fact]
    public void FooterReason_EmptyHidesLine()
    {
        var service = new DefaultEmailTemplateService(Localizer(), new StaticMonitor(new EmailBrandingOptions { FooterReason = "" }));

        var message = service.BuildPasswordResetEmail("a@b.com", "https://x.test/r", "App");

        message.HtmlBody.Should().NotContain("You are receiving this email");
        message.TextBody.Should().NotContain("You are receiving this email");
    }

    [Fact]
    public void FooterReason_DefaultIsNeutralAndLocalized()
    {
        var message = Plain().BuildPasswordResetEmail("a@b.com", "https://x.test/r", "App");

        message.HtmlBody.Should().Contain("You are receiving this email because of a request involving your email address.");
    }
}
