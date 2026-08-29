using AQ.Identity.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

/// <summary>
/// Prevents removing the manage_api claim from the last administrator — directly (claim
/// upsert/delete) or indirectly (deactivating that user, which is practically equivalent
/// since a deactivated user is rejected at both token validation and sign-in) — which would
/// otherwise silently re-open the unauthenticated initial-setup flow (SetupStateService
/// treats "no manage_api claim exists" as "setup required"). Used by
/// UpsertUserClaimsEndpoint, DeleteUserClaimTypeEndpoint, and SetUserActiveEndpoint; any
/// future account-deletion endpoint must call this too before allowing a self-delete.
/// </summary>
public static class AdminClaimGuard
{
    public const string ManageApiClaimType = "manage_api";

    /// <summary>
    /// Returns true if removing the manage_api claim from <paramref name="userId"/> would
    /// leave zero administrators.
    /// </summary>
    public static async Task<bool> WouldRemoveLastAdminAsync(
        IIdentityDbContext context,
        Guid userId,
        CancellationToken ct)
    {
        var otherAdminCount = await context.StoredClaims
            .CountAsync(c => c.Type == ManageApiClaimType && c.UserId != userId, ct);

        return otherAdminCount == 0;
    }
}
