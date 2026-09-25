using System.Globalization;
using System.Net;
using AQ.Utilities.Email.Resources;
using Microsoft.Extensions.Localization;

namespace AQ.Utilities.Email;

public class DefaultEmailTemplateService(IStringLocalizer<EmailResource> localizer) : IEmailTemplateService
{
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

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""{effectiveCulture.Name}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 20px 0;"">
                <table role=""presentation"" style=""width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h1 style=""color: #333333; font-size: 24px; margin: 0 0 20px 0;"">{heading}</h1>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                {intro}
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background-color: #007bff; border-radius: 4px; text-align: center;"">
                                        <a href=""{verificationUrl}"" style=""display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px;"">{button}</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                {fallbackLink}
                            </p>
                            <p style=""color: #007bff; font-size: 14px; word-break: break-all; margin: 10px 0 0 0;"">
                                {verificationUrl}
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 30px 0 0 0; border-top: 1px solid #eeeeee; padding-top: 20px;"">
                                {ignore}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["Verification.Text"], appName, verificationUrl);
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

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""{effectiveCulture.Name}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 20px 0;"">
                <table role=""presentation"" style=""width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h1 style=""color: #333333; font-size: 24px; margin: 0 0 20px 0;"">{heading}</h1>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                {intro}
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background-color: #dc3545; border-radius: 4px; text-align: center;"">
                                        <a href=""{resetUrl}"" style=""display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px;"">{button}</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                {fallbackLink}
                            </p>
                            <p style=""color: #dc3545; font-size: 14px; word-break: break-all; margin: 10px 0 0 0;"">
                                {resetUrl}
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 30px 0 0 0; border-top: 1px solid #eeeeee; padding-top: 20px;"">
                                {ignore}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["PasswordReset.Text"], appName, resetUrl);
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

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""{effectiveCulture.Name}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 20px 0;"">
                <table role=""presentation"" style=""width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h1 style=""color: #333333; font-size: 24px; margin: 0 0 20px 0;"">{heading}</h1>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                {intro}
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background-color: #007bff; border-radius: 4px; text-align: center;"">
                                        <a href=""{acceptUrl}"" style=""display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px;"">{button}</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                {fallbackLink}
                            </p>
                            <p style=""color: #007bff; font-size: 14px; word-break: break-all; margin: 10px 0 0 0;"">
                                {acceptUrl}
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 30px 0 0 0; border-top: 1px solid #eeeeee; padding-top: 20px;"">
                                {ignore}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["WorkspaceInvitation.Text"], inviterName, workspaceName, appName, acceptUrl);
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

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""{effectiveCulture.Name}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 20px 0;"">
                <table role=""presentation"" style=""width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h1 style=""color: #333333; font-size: 24px; margin: 0 0 20px 0;"">{heading}</h1>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                {intro}
                            </p>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                {notYou}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = string.Format(CultureInfo.InvariantCulture, localizer["SecurityAlert.Text"], eventDescription, appName);
        var subject = string.Format(CultureInfo.InvariantCulture, localizer["SecurityAlert.Subject"], appName);

        return new EmailMessage(toEmail, subject, htmlBody, textBody);
    }
}
