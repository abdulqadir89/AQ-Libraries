using System.Security.Claims;
using AQ.Actors;

namespace AQ.Identity.Core.Auditing;

/// <summary>Resolves the <see cref="Actor"/> behind a signed-in admin request (cookie page or bearer API).</summary>
// RULE OID-9
public static class AuditActors
{
    public static Actor? TryGet(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var id))
        {
            return null;
        }

        return user.FindFirstValue(PrincipalClaims.Type) == PrincipalClaims.Service
            ? new Actor(id, PrincipalKind.Service, ClientId: user.FindFirstValue(PrincipalClaims.ClientId))
            : new Actor(id, PrincipalKind.Human);
    }

    /// <summary>Like <see cref="TryGet"/> but throws: an admin action with no identifiable actor must not be audited as anonymous.</summary>
    public static Actor Require(ClaimsPrincipal user) =>
        TryGet(user) ?? throw new InvalidOperationException("The signed-in principal has no GUID subject, so the audit actor cannot be determined.");
}
