using AQ.Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace AQ.Identity.UI.Extensions;

internal static class UserManagerExtensions
{
    /// <summary>Records a successful interactive sign-in so admin screens can show the last login time.</summary>
    public static async Task RecordLoginAsync(this UserManager<ApplicationUser> userManager, ApplicationUser? user)
    {
        if (user == null) return;

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
    }
}
