using Microsoft.Extensions.Logging;

namespace AQ.Utilities.Email;

public class ConsoleEmailService(ILogger<ConsoleEmailService> logger) : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogDebug(
            "Email sent. Subject: {Subject}, To: {To}, HtmlBody: {HtmlBody}",
            message.Subject,
            message.To,
            message.HtmlBody);

        return Task.CompletedTask;
    }
}
