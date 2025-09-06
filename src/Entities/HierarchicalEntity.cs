using AQ.Abstractions;
using AQ.Utilities.Search;

namespace AQ.Entities;

/// <summary>
/// Abstract base class for entities that form hierarchical structures.
/// Provides common functionality for self-referential parent-child relationships.
/// </summary>
/// <typeparam name="T">The concrete entity type implementing the hierarchy</typeparam>
public abstract class HierarchicalEntity<T> : Entity where T : HierarchicalEntity<T>, IHierarchicalEntity
{
    [Searchable(Weight = 3.0)]
    public string Name { get; protected set; } = default!;
    [Searchable]
    public string Slug { get; protected set; } = default!;
    public int Level { get; protected set; }
    [Searchable]
    public string HierarchyPath { get; protected set; } = default!;

    // Self-referential for n-level hierarchy
    public Guid? ParentId { get; protected set; }
    public T? Parent { get; protected set; }
    public ICollection<T> Children { get; protected set; } = default!;

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

    protected void SetHierarchyProperties(string name, T? parent, IEnumerable<T>? allDescendants = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Store old hierarchy path to detect changes
        var oldHierarchyPath = HierarchyPath;

        Name = name.Trim();
        Slug = GenerateSlug(name);

        if (parent is null)
        {
            Level = 0;
            HierarchyPath = $"/{Slug}";
            ParentId = null;
            Parent = parent;
        }
        else
        {
            Level = parent.Level + 1;
            HierarchyPath = $"{parent.HierarchyPath}/{Slug}";
            ParentId = parent.Id;
            Parent = parent;
        }

        // If hierarchy path changed and descendants are provided, update all descendants
        if (!string.IsNullOrEmpty(oldHierarchyPath) &&
            oldHierarchyPath != HierarchyPath &&
            allDescendants != null)
        {
            UpdateDescendantHierarchyPaths(oldHierarchyPath, allDescendants);
        }
    }

    private void UpdateDescendantHierarchyPaths(string oldHierarchyPath, IEnumerable<T> allDescendants)
    {
        // Find all descendants that start with the old hierarchy path
        var descendantsToUpdate = allDescendants
            .Where(d => d.HierarchyPath.StartsWith(oldHierarchyPath + "/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var descendant in descendantsToUpdate)
        {
            // Replace the old hierarchy path prefix with the new one
            var remainingPath = descendant.HierarchyPath.Substring(oldHierarchyPath.Length);
            descendant.HierarchyPath = HierarchyPath + remainingPath;

            // Update the level based on the new hierarchy depth
            descendant.Level = descendant.HierarchyPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length - 1;
        }
    }
}
