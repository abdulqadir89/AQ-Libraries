using AQ.Identity.Core.Abstractions;

namespace AQ.Identity.Email;

public class DefaultEmailTemplateService : IEmailTemplateService
{
    public EmailMessage BuildVerificationEmail(string toEmail, string verificationUrl, string appName)
    {
        var htmlBody = $@"<!DOCTYPE html>
<html>
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
                            <h1 style=""color: #333333; font-size: 24px; margin: 0 0 20px 0;"">Verify your email address</h1>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                Thank you for signing up with <strong>{appName}</strong>. Please verify your email address by clicking the button below.
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background-color: #007bff; border-radius: 4px; text-align: center;"">
                                        <a href=""{verificationUrl}"" style=""display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px;"">Verify Email</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                If the button doesn't work, copy and paste this link into your browser:
                            </p>
                            <p style=""color: #007bff; font-size: 14px; word-break: break-all; margin: 10px 0 0 0;"">
                                {verificationUrl}
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 30px 0 0 0; border-top: 1px solid #eeeeee; padding-top: 20px;"">
                                If you didn't create an account with {appName}, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = $"Verify your email address for {appName}\n\nPlease verify your email by visiting the following link:\n{verificationUrl}\n\nIf you didn't create an account with {appName}, you can safely ignore this email.";

        return new EmailMessage(toEmail, $"Verify your email - {appName}", htmlBody, textBody);
    }

    public EmailMessage BuildPasswordResetEmail(string toEmail, string resetUrl, string appName)
    {
        var htmlBody = $@"<!DOCTYPE html>
<html>
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
                            <h1 style=""color: #333333; font-size: 24px; margin: 0 0 20px 0;"">Reset your password</h1>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                We received a request to reset your password for <strong>{appName}</strong>. Click the button below to create a new password.
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background-color: #dc3545; border-radius: 4px; text-align: center;"">
                                        <a href=""{resetUrl}"" style=""display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px;"">Reset Password</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                If the button doesn't work, copy and paste this link into your browser:
                            </p>
                            <p style=""color: #dc3545; font-size: 14px; word-break: break-all; margin: 10px 0 0 0;"">
                                {resetUrl}
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 30px 0 0 0; border-top: 1px solid #eeeeee; padding-top: 20px;"">
                                If you didn't request a password reset, you can safely ignore this email. Your password will not be changed.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = $"Reset your password for {appName}\n\nWe received a request to reset your password. Please visit the following link to create a new password:\n{resetUrl}\n\nIf you didn't request a password reset, you can safely ignore this email. Your password will not be changed.";

        return new EmailMessage(toEmail, $"Reset your password - {appName}", htmlBody, textBody);
    }
}
