namespace AQ.Identity.Core.Abstractions;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);
