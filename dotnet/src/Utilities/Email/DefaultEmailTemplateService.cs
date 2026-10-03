using System.Globalization;
using System.Net;
using AQ.Utilities.Email.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AQ.Utilities.Email;

public class DefaultEmailTemplateService(
    IStringLocalizer<EmailResource> localizer,
    IOptionsMonitor<EmailBrandingOptions>? brandingOptions = null) : IEmailTemplateService
{
    private EmailBrandingOptions Branding => brandingOptions?.CurrentValue ?? new EmailBrandingOptions();

    /// <summary>
    /// Temporarily sets <see cref="CultureInfo.CurrentUICulture"/> for the duration of a template
    /// build, restoring the previous value on dispose. A null culture is a no-op, so callers that
    /// don't pass one keep using the ambient request culture (e.g. the caller's Accept-Language).
    /// </summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo? _previous;
        private readonly bool _active;

        public CultureScope(CultureInfo? culture)
        {
            _active = culture is not null;
            if (!_active)
            {
                return;
            }

            _previous = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = culture!;
        }

        public void Dispose()
        {
            if (_active)
            {
                CultureInfo.CurrentUICulture = _previous!;
            }
        }
    }

    public EmailMessage BuildVerificationEmail(string toEmail, string verificationUrl, string appName, CultureInfo? culture = null)
    {
        using var _ = new CultureScope(culture);

        var effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
        var encodedAppName = WebUtility.HtmlEncode(appName);

        var heading = localizer["Verification.Heading"];
        var intro = string.Format(CultureInfo.InvariantCulture, localizer["Verification.Intro"], encodedAppName);
        var button = localizer["Verification.Button"];
        var fallbackLink = localizer["Verification.FallbackLink"];
        var ignore = string.Format(CultureInfo.InvariantCulture, localizer["Verification.Ignore"], encodedAppName);

        var htmlBody = EmailLayout.Render(
            new EmailLayoutContent(effectiveCulture.Name, appName, heading, intro, verificationUrl, button, fallbackLink, ignore),
            Branding, localizer);

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["Verification.Text"], appName, verificationUrl) + EmailLayout.TextFooter(appName, Branding, localizer);
        var subject = string.Format(CultureInfo.InvariantCulture, localizer["Verification.Subject"], appName);

        return new EmailMessage(toEmail, subject, htmlBody, textBody);
    }

    public EmailMessage BuildPasswordResetEmail(string toEmail, string resetUrl, string appName, CultureInfo? culture = null)
    {
        using var _ = new CultureScope(culture);

        var effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
        var encodedAppName = WebUtility.HtmlEncode(appName);

        var heading = localizer["PasswordReset.Heading"];
        var intro = string.Format(CultureInfo.InvariantCulture, localizer["PasswordReset.Intro"], encodedAppName);
        var button = localizer["PasswordReset.Button"];
        var fallbackLink = localizer["PasswordReset.FallbackLink"];
        var ignore = localizer["PasswordReset.Ignore"];

        var htmlBody = EmailLayout.Render(
            new EmailLayoutContent(effectiveCulture.Name, appName, heading, intro, resetUrl, button, fallbackLink, ignore),
            Branding, localizer);

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["PasswordReset.Text"], appName, resetUrl) + EmailLayout.TextFooter(appName, Branding, localizer);
        var subject = string.Format(CultureInfo.InvariantCulture, localizer["PasswordReset.Subject"], appName);

        return new EmailMessage(toEmail, subject, htmlBody, textBody);
    }

    public EmailMessage BuildWorkspaceInvitationEmail(string toEmail, string acceptUrl, string workspaceName, string inviterName, string appName, CultureInfo? culture = null)
    {
        using var _ = new CultureScope(culture);

        var effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
        var encodedWorkspaceName = WebUtility.HtmlEncode(workspaceName);
        var encodedInviterName = WebUtility.HtmlEncode(inviterName);
        var encodedAppName = WebUtility.HtmlEncode(appName);

        var heading = string.Format(CultureInfo.InvariantCulture, localizer["WorkspaceInvitation.Heading"], encodedWorkspaceName);
        var intro = string.Format(CultureInfo.InvariantCulture, localizer["WorkspaceInvitation.Intro"], encodedInviterName, encodedWorkspaceName, encodedAppName);
        var button = localizer["WorkspaceInvitation.Button"];
        var fallbackLink = localizer["WorkspaceInvitation.FallbackLink"];
        var ignore = localizer["WorkspaceInvitation.Ignore"];

        var htmlBody = EmailLayout.Render(
            new EmailLayoutContent(effectiveCulture.Name, appName, heading, intro, acceptUrl, button, fallbackLink, ignore),
            Branding, localizer);

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["WorkspaceInvitation.Text"], inviterName, workspaceName, appName, acceptUrl) + EmailLayout.TextFooter(appName, Branding, localizer);
        var subject = string.Format(CultureInfo.InvariantCulture, localizer["WorkspaceInvitation.Subject"], workspaceName, appName);

        return new EmailMessage(toEmail, subject, htmlBody, textBody);
    }

    public EmailMessage BuildSecurityAlertEmail(string toEmail, string eventDescription, string appName, CultureInfo? culture = null)
    {
        using var _ = new CultureScope(culture);

        var effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
        var encodedAppName = WebUtility.HtmlEncode(appName);
        var encodedEventDescription = WebUtility.HtmlEncode(eventDescription);

        var heading = localizer["SecurityAlert.Heading"];
        var intro = string.Format(CultureInfo.InvariantCulture, localizer["SecurityAlert.Intro"], encodedEventDescription, encodedAppName);
        var notYou = localizer["SecurityAlert.NotYou"];

        var htmlBody = EmailLayout.Render(
            new EmailLayoutContent(effectiveCulture.Name, appName, heading, intro, FinePrintHtml: notYou),
            Branding, localizer);

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["SecurityAlert.Text"], eventDescription, appName) + EmailLayout.TextFooter(appName, Branding, localizer);
        var subject = string.Format(CultureInfo.InvariantCulture, localizer["SecurityAlert.Subject"], appName);

        return new EmailMessage(toEmail, subject, htmlBody, textBody);
    }
}
