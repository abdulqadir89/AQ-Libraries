using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AQ.Utilities.Email;

public class SmtpEmailService(
    IOptionsMonitor<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var value = options.CurrentValue;
        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(value.Host, value.Port,
                value.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

            if (!string.IsNullOrEmpty(value.Username) && !string.IsNullOrEmpty(value.Password))
            {
                await smtpClient.AuthenticateAsync(value.Username, value.Password, ct);
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(value.FromName, value.FromAddress));
            mimeMessage.To.Add(new MailboxAddress(message.To, message.To));
            mimeMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody
            };

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            await smtpClient.SendAsync(mimeMessage, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email. Subject: {Subject}, Recipient: {Recipient}",
                message.Subject, message.To);
            throw;
        }
        finally
        {
            await smtpClient.DisconnectAsync(true, ct);
        }
    }
}
