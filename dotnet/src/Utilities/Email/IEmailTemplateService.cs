namespace AQ.Utilities.Email;

public interface IEmailTemplateService
{
    EmailMessage BuildVerificationEmail(string toEmail, string verificationUrl, string appName);
    EmailMessage BuildPasswordResetEmail(string toEmail, string resetUrl, string appName);
    EmailMessage BuildWorkspaceInvitationEmail(string toEmail, string acceptUrl, string workspaceName, string inviterName, string appName);
}
