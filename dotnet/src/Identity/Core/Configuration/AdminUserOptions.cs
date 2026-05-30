namespace AQ.Identity.Core.Configuration;

public class AdminUserOptions
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FullName { get; set; } = "Administrator";
}
