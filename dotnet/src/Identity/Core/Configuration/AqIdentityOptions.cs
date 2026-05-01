namespace AQ.Identity.Core.Configuration;

public class AqIdentityOptions
{
    public string Issuer { get; set; } = default!;
    public string AppName { get; set; } = "AQ Identity";
    public TokenLifetimeOptions Tokens { get; set; } = new();
    public PasswordPolicyOptions Password { get; set; } = new();
    public LockoutPolicyOptions Lockout { get; set; } = new();
    public KeyManagementOptions Keys { get; set; } = new();
    public EmailOptions Email { get; set; } = new();
    public GoogleOptions? Google { get; set; }
}
