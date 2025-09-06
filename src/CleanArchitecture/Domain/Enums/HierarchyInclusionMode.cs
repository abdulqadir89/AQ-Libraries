namespace AQ.Domain.Enums;

/// <summary>
/// Defines how hierarchical entities should be included when applying search and filter operations
/// </summary>
public enum HierarchyInclusionMode
{
    /// <summary>
    /// No hierarchical inclusion - only directly matching entities are returned
    /// </summary>
    None = 0,

    /// <summary>
    /// Include ancestors (parent entities) when children match the criteria.
    /// This maintains the hierarchical structure in results by showing parent contexts.
    /// </summary>
    IncludeAncestors = 1,

    /// <summary>
    /// Include descendants (child entities) when parents match the criteria.
    /// This shows all children under matching parent entities.
    /// </summary>
    IncludeDescendants = 2,

    /// <summary>
    /// Include complete hierarchy tree (both ancestors and descendants).
    /// This provides the full hierarchical context around matching entities.
    /// </summary>
    IncludeHierarchyTree = 3
}
