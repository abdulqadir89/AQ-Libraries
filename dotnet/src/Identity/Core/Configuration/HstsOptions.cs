namespace AQ.Identity.Core.Configuration;

/// <summary>
/// Configures ASP.NET Core's built-in HSTS middleware (<c>services.AddHsts()</c> /
/// <c>app.UseHsts()</c>), per RFC 6797 and OWASP secure-headers guidance. Only applied when the
/// host environment is not Development (HSTS breaks plain-HTTP local dev).
/// </summary>
public class HstsOptions
{
    public int MaxAgeDays { get; set; } = 365;
    public bool IncludeSubDomains { get; set; } = true;

    /// <summary>
    /// Opts into HSTS preload list submission (hstspreload.org). Off by default: preload is
    /// effectively irreversible in practice (browser-shipped list, slow to roll back), so it
    /// must be a deliberate, informed choice by the consuming app, not a library default.
    /// </summary>
    public bool Preload { get; set; }
}
