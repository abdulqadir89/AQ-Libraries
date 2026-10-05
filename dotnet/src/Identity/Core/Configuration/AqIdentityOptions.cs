using AQ.Utilities.Email;

namespace AQ.Identity.Core.Configuration;

public class AqIdentityOptions
{
    public string Issuer { get; set; } = default!;
    public string AppName { get; set; } = "AQ Identity";
    public BrandingOptions Branding { get; set; } = new();
    public TokenLifetimeOptions Tokens { get; set; } = new();
    public PasswordPolicyOptions Password { get; set; } = new();
    public LockoutPolicyOptions Lockout { get; set; } = new();
    public KeyManagementOptions Keys { get; set; } = new();
    public HstsOptions Hsts { get; set; } = new();
    public EmailOptions Email { get; set; } = new();
    public GoogleOptions? Google { get; set; }
    public AdminUserOptions? AdminUser { get; set; }

    /// <summary>
    /// Audiences the IdP's own bearer-protected endpoints accept (OpenIddict validation
    /// AddAudiences). Must match the resources of the scopes that grant access to them.
    /// Empty = audience not checked.
    /// </summary>
    public List<string> Audiences { get; set; } = [];
}
