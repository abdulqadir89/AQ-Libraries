namespace AQ.Common.Domain.Entities;

/// <summary>
/// Abstract base class for entities that form hierarchical structures.
/// Provides common functionality for self-referential parent-child relationships.
/// </summary>
/// <typeparam name="T">The concrete entity type implementing the hierarchy</typeparam>
public abstract class HierarchicalEntity<T> : AuditableEntity where T : HierarchicalEntity<T>
{
    public string Name { get; protected set; } = default!;
    public string? Description { get; protected set; }
    public string Slug { get; protected set; } = default!;
    public int Level { get; protected set; }
    public string HierarchyPath { get; protected set; } = default!;

    // Self-referential for n-level hierarchy
    public Guid? ParentId { get; protected set; }
    public T? Parent { get; protected set; }
    public IReadOnlyList<T> Children { get; protected set; } = [];

    protected HierarchicalEntity() { }

    public bool IsRoot => ParentId is null;

    public bool IsLeaf => !Children.Any();

    public bool IsAncestorOf(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        return entity.HierarchyPath.StartsWith(HierarchyPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDescendantOf(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        return entity.IsAncestorOf((T)this);
    }

    public IReadOnlyList<string> GetAncestorSlugs()
    {
        var parts = HierarchyPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Take(parts.Length - 1).ToList();
    }

    public IReadOnlyList<string> GetHierarchyParts()
    {
        return HierarchyPath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public string GetDisplayPath(string separator = " > ")
    {
        var parts = GetHierarchyParts();
        return string.Join(separator, parts.Select(FormatDisplayName));
    }

    protected static string GenerateSlug(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return name.ToLowerInvariant()
                  .Replace(' ', '-')
                  .Replace("&", "and")
                  .Where(c => char.IsLetterOrDigit(c) || c == '-')
                  .Aggregate("", (current, c) => current + c)
                  .Trim('-');
    }

    protected virtual string FormatDisplayName(string slug)
    {
        // Convert slug back to display name (override in derived classes if needed)
        return slug.Replace('-', ' ')
                  .Split(' ')
                  .Select(word => char.ToUpperInvariant(word[0]) + word[1..])
                  .Aggregate((current, next) => current + " " + next);
    }

    protected void SetHierarchyProperties(string name, T? parent, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name;
        Description = description;
        Slug = GenerateSlug(name);

        if (parent is null)
        {
            Level = 0;
            HierarchyPath = $"/{Slug}";
            ParentId = null;
            Parent = null;
        }
        else
        {
            Level = parent.Level + 1;
            HierarchyPath = $"{parent.HierarchyPath}/{Slug}";
            ParentId = parent.Id;
            Parent = parent;
        }
    }
}
