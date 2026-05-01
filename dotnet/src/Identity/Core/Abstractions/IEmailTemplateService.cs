namespace AQ.Identity.Core.Abstractions;

public interface IEmailTemplateService
{
    EmailMessage BuildVerificationEmail(string toEmail, string verificationUrl, string appName);
    EmailMessage BuildPasswordResetEmail(string toEmail, string resetUrl, string appName);
}
