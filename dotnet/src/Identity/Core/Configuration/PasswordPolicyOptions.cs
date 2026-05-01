namespace AQ.Identity.Core.Configuration;

public class PasswordPolicyOptions
{
    public int MinLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireUppercase { get; set; } = false;
    public bool RequireNonAlphanumeric { get; set; } = false;
}
