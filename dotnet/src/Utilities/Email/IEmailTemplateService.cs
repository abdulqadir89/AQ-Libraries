using System.Globalization;

namespace AQ.Utilities.Email;

public interface IEmailTemplateService
{
    EmailMessage BuildVerificationEmail(string toEmail, string verificationUrl, string appName, CultureInfo? culture = null);
    EmailMessage BuildPasswordResetEmail(string toEmail, string resetUrl, string appName, CultureInfo? culture = null);
    EmailMessage BuildWorkspaceInvitationEmail(string toEmail, string acceptUrl, string workspaceName, string inviterName, string appName, CultureInfo? culture = null);

    /// <summary>
    /// A security-relevant account change notification (password changed, 2FA disabled,
    /// etc). Lets the user recognize and act on a change they didn't make.
    /// </summary>
    EmailMessage BuildSecurityAlertEmail(string toEmail, string eventDescription, string appName, CultureInfo? culture = null);
}
