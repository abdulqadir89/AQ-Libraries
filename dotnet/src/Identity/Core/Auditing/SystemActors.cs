using AQ.Actors;

namespace AQ.Identity.Core.Auditing;

/// <summary>Platform actors for work the identity server does on its own.</summary>
public static class IdentitySystemActors
{
    /// <summary>Signing key rotation worker.</summary>
    public static readonly Actor KeyRotation = new(
        new Guid("00000000-0000-0000-0002-000000000001"), PrincipalKind.System);
}
