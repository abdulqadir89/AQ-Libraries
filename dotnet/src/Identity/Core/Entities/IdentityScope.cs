namespace AQ.Identity.Core.Entities;

public class IdentityScope
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ICollection<ScopeClaimType> ClaimTypes { get; private set; } = [];

    public IdentityScope() { }

    private IdentityScope(string name, string displayName, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayName = displayName;
        Description = description;
    }

    public static IdentityScope Create(string name, string displayName, string description)
        => new(name, displayName, description);

    public void Update(string displayName, string description)
    {
        DisplayName = displayName;
        Description = description;
    }
}
