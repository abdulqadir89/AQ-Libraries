namespace AQ.Identity.Core.Entities;

public class ScopeClaimType
{
    public Guid Id { get; private set; }

    public Guid ScopeId { get; private set; }

    public string ClaimType { get; private set; } = string.Empty;

    public IdentityScope Scope { get; private set; } = null!;

    public ScopeClaimType() { }

    private ScopeClaimType(Guid scopeId, string claimType)
    {
        Id = Guid.NewGuid();
        ScopeId = scopeId;
        ClaimType = claimType;
    }

    public static ScopeClaimType Create(Guid scopeId, string claimType)
        => new(scopeId, claimType);
}
