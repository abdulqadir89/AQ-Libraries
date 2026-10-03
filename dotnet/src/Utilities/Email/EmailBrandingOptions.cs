namespace AQ.Utilities.Email;

/// <summary>
/// Visual branding applied to every templated email. All values are optional; the defaults
/// give a neutral, unbranded look. Consumers supply their own values (typically from the
/// <c>Branding</c> sub-section of the email configuration section).
/// </summary>
public class EmailBrandingOptions
{
    /// <summary>Absolute https URL of a PNG/JPG logo (email clients block SVG and web fonts). When null, the app name is shown as text.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Logo alt text. Defaults to the app name.</summary>
    public string? LogoAlt { get; set; }

    /// <summary>Rendered logo width in pixels.</summary>
    public int LogoWidth { get; set; } = 160;

    /// <summary>Accent colour: top bar, button background, links, text wordmark.</summary>
    public string PrimaryColor { get; set; } = "#007bff";

    /// <summary>Text colour on top of <see cref="PrimaryColor"/> (button label).</summary>
    public string PrimaryTextColor { get; set; } = "#ffffff";

    /// <summary>Heading colour.</summary>
    public string HeadingColor { get; set; } = "#333333";

    /// <summary>Body text colour.</summary>
    public string TextColor { get; set; } = "#666666";

    /// <summary>Muted text colour (footer, fine print).</summary>
    public string MutedColor { get; set; } = "#999999";

    /// <summary>Page background colour.</summary>
    public string BackgroundColor { get; set; } = "#f4f4f4";

    /// <summary>Card background colour.</summary>
    public string CardColor { get; set; } = "#ffffff";

    /// <summary>CSS font stack.</summary>
    public string FontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>Short tagline shown in the footer.</summary>
    public string? Tagline { get; set; }

    /// <summary>Public website URL, linked in the footer.</summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>Support address shown in the footer ("Need help?").</summary>
    public string? SupportEmail { get; set; }

    /// <summary>
    /// Footer line explaining why the recipient got the email. <c>{0}</c> is replaced with the app name.
    /// Null uses the built-in localized wording; an empty string hides the line. A custom value is
    /// used as-is for every language, so leave it null to keep translations.
    /// </summary>
    public string? FooterReason { get; set; }

    /// <summary>Extra footer line, e.g. company name or postal address.</summary>
    public string? FooterText { get; set; }
}
