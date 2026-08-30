namespace AQ.Identity.Core.Configuration;

/// <summary>
/// Lets a consuming app white-label the shared login/register/account/admin UI without
/// forking it. Color theming is handled by the existing named-theme system
/// (<c>[data-theme]</c> in <c>tailwind.input.css</c>, picked via <c>_ThemePicker</c>) — this
/// only configures which theme is the default and swaps the text wordmark for a logo image.
/// </summary>
public class BrandingOptions
{
    /// <summary>Null falls back to the text wordmark (<c>AqIdentityOptions.AppName</c>).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Theme used when the user has no <c>aq-theme</c> cookie yet. Must be one of
    /// <c>ThemeModel.ValidThemes</c> ("clay", "blue", "slate", "zinc", "bloom", "spark");
    /// an unrecognized value falls through to "clay" the same way an invalid cookie value does.
    /// </summary>
    public string DefaultTheme { get; set; } = "clay";
}
