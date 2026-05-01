using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AQ.Identity.Email;

public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(options.Value.Host, options.Value.Port,
                options.Value.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

            if (!string.IsNullOrEmpty(options.Value.Username) && !string.IsNullOrEmpty(options.Value.Password))
            {
                await smtpClient.AuthenticateAsync(options.Value.Username, options.Value.Password, ct);
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(options.Value.FromName, options.Value.FromAddress));
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