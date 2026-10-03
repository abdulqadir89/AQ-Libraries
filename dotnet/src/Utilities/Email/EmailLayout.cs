using System.Globalization;
using System.Net;
using System.Text;
using AQ.Utilities.Email.Resources;
using Microsoft.Extensions.Localization;

namespace AQ.Utilities.Email;

/// <summary>Content slots for <see cref="EmailLayout"/>. Text fields are expected to be already HTML-encoded.</summary>
internal sealed record EmailLayoutContent(
    string Culture,
    string AppName,
    string Heading,
    string IntroHtml,
    string? ButtonUrl = null,
    string? ButtonLabel = null,
    string? FallbackLinkText = null,
    string? FinePrintHtml = null);

/// <summary>
/// Branded, email-client-safe (table-based, inline CSS) shell shared by all templated emails.
/// Appearance comes entirely from <see cref="EmailBrandingOptions"/>.
/// </summary>
internal static class EmailLayout
{
    public static string Render(EmailLayoutContent c, EmailBrandingOptions b, IStringLocalizer<EmailResource> localizer)
    {
        var appName = WebUtility.HtmlEncode(c.AppName);
        var sb = new StringBuilder();

        sb.Append($@"<!DOCTYPE html>
<html lang=""{Attr(c.Culture)}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{appName}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: {Attr(b.FontFamily)}; background-color: {Attr(b.BackgroundColor)};"">
    <div style=""display: none; max-height: 0; overflow: hidden; opacity: 0; mso-hide: all;"">{c.IntroHtml}</div>
    <table role=""presentation"" width=""100%"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 24px 12px;"">
                <table role=""presentation"" width=""600"" style=""width: 100%; max-width: 600px; border-collapse: collapse;"">
                    <tr>
                        <td align=""center"" style=""padding: 8px 0 20px 0;"">
                            {Header(b, c.AppName, appName)}
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: {Attr(b.CardColor)}; border-top: 4px solid {Attr(b.PrimaryColor)}; border-radius: 8px; padding: 36px 32px;"">
                            <h1 style=""color: {Attr(b.HeadingColor)}; font-size: 24px; line-height: 1.3; margin: 0 0 20px 0;"">{c.Heading}</h1>
                            <p style=""color: {Attr(b.TextColor)}; font-size: 16px; line-height: 1.5; margin: 0 0 20px 0;"">
                                {c.IntroHtml}
                            </p>");

        if (!string.IsNullOrEmpty(c.ButtonUrl))
        {
            var url = Attr(c.ButtonUrl);
            sb.Append($@"
                            <table role=""presentation"" style=""margin: 28px 0; border-collapse: collapse;"">
                                <tr>
                                    <td style=""background-color: {Attr(b.PrimaryColor)}; border-radius: 6px; text-align: center;"">
                                        <a href=""{url}"" style=""display: inline-block; padding: 14px 28px; color: {Attr(b.PrimaryTextColor)}; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 6px;"">{c.ButtonLabel}</a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: {Attr(b.TextColor)}; font-size: 14px; line-height: 1.5; margin: 20px 0 0 0;"">
                                {c.FallbackLinkText}
                            </p>
                            <p style=""font-size: 14px; word-break: break-all; margin: 8px 0 0 0;"">
                                <a href=""{url}"" style=""color: {Attr(b.PrimaryColor)};"">{url}</a>
                            </p>");
        }

        if (!string.IsNullOrEmpty(c.FinePrintHtml))
        {
            sb.Append($@"
                            <p style=""color: {Attr(b.MutedColor)}; font-size: 13px; line-height: 1.5; margin: 28px 0 0 0; border-top: 1px solid {Attr(b.BackgroundColor)}; padding-top: 20px;"">
                                {c.FinePrintHtml}
                            </p>");
        }

        sb.Append($@"
                        </td>
                    </tr>
                    <tr>
                        <td align=""center"" style=""padding: 20px 16px 0 16px; color: {Attr(b.MutedColor)}; font-size: 12px; line-height: 1.6;"">
                            {Footer(c, b, localizer, appName)}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

        return sb.ToString();
    }

    /// <summary>Plain-text footer lines matching the HTML footer.</summary>
    public static string TextFooter(string appName, EmailBrandingOptions b, IStringLocalizer<EmailResource> localizer)
    {
        var lines = new List<string>();
        if (Reason(b, localizer, appName) is { } reason)
        {
            lines.Add(reason);
        }

        if (!string.IsNullOrWhiteSpace(b.SupportEmail))
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, localizer["Layout.Help"], b.SupportEmail));
        }

        var identity = string.Join(" | ", new[] { appName, b.Tagline, b.WebsiteUrl }.Where(s => !string.IsNullOrWhiteSpace(s)));
        lines.Add(identity);

        if (!string.IsNullOrWhiteSpace(b.FooterText))
        {
            lines.Add(b.FooterText);
        }

        return "\n\n--\n" + string.Join("\n", lines);
    }

    /// <summary>Raw (unencoded) footer reason: configured override, localized default, or null when hidden.</summary>
    private static string? Reason(EmailBrandingOptions b, IStringLocalizer<EmailResource> localizer, string rawAppName)
    {
        var template = b.FooterReason ?? localizer["Layout.Reason"].Value;
        return string.IsNullOrWhiteSpace(template)
            ? null
            : string.Format(CultureInfo.InvariantCulture, template, rawAppName);
    }

    private static string Header(EmailBrandingOptions b, string rawAppName, string encodedAppName)
    {
        if (!string.IsNullOrWhiteSpace(b.LogoUrl))
        {
            var alt = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(b.LogoAlt) ? rawAppName : b.LogoAlt);
            return $@"<img src=""{Attr(b.LogoUrl)}"" alt=""{alt}"" width=""{b.LogoWidth}"" style=""display: block; border: 0; width: {b.LogoWidth}px; max-width: 100%; height: auto; color: {Attr(b.PrimaryColor)}; font-size: 24px; font-weight: bold;"">";
        }

        return $@"<span style=""color: {Attr(b.PrimaryColor)}; font-size: 24px; font-weight: bold;"">{encodedAppName}</span>";
    }

    private static string Footer(EmailLayoutContent c, EmailBrandingOptions b, IStringLocalizer<EmailResource> localizer, string appName)
    {
        var parts = new List<string>();
        if (Reason(b, localizer, c.AppName) is { } reason)
        {
            parts.Add(WebUtility.HtmlEncode(reason));
        }

        if (!string.IsNullOrWhiteSpace(b.SupportEmail))
        {
            var support = WebUtility.HtmlEncode(b.SupportEmail);
            var link = $@"<a href=""mailto:{support}"" style=""color: {Attr(b.PrimaryColor)};"">{support}</a>";
            parts.Add(string.Format(CultureInfo.InvariantCulture, localizer["Layout.Help"], link));
        }

        var identity = new List<string> { $"<strong>{appName}</strong>" };
        if (!string.IsNullOrWhiteSpace(b.Tagline))
        {
            identity.Add(WebUtility.HtmlEncode(b.Tagline));
        }

        if (!string.IsNullOrWhiteSpace(b.WebsiteUrl))
        {
            var site = WebUtility.HtmlEncode(b.WebsiteUrl);
            identity.Add($@"<a href=""{site}"" style=""color: {Attr(b.PrimaryColor)};"">{site}</a>");
        }

        parts.Add(string.Join(" &middot; ", identity));

        if (!string.IsNullOrWhiteSpace(b.FooterText))
        {
            parts.Add(WebUtility.HtmlEncode(b.FooterText));
        }

        return string.Join("<br>", parts);
    }

    /// <summary>Escapes a value for use inside a double-quoted HTML attribute (ampersands are left as-is so URLs stay readable).</summary>
    private static string Attr(string value) =>
        value.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
}
