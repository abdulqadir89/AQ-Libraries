using Microsoft.AspNetCore.Identity;

namespace AQ.Identity.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationUser() { }

    public ApplicationUser(string email, string fullName)
    {
        Email = email;
        UserName = email;
        FullName = fullName;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static ApplicationUser Create(string email, string fullName)
    {
        return new ApplicationUser(email, fullName);
    }
}
