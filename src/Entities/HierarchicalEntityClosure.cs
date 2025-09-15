using System.ComponentModel.DataAnnotations.Schema;
using AQ.Abstractions;

namespace AQ.Entities;

/// <summary>
/// Closure table for hierarchical entities. Used for fast ancestor/descendant queries.
/// Each row represents a path from Ancestor to Descendant with a given Depth.
/// </summary>
[NotMapped]
public abstract class HierarchicalEntityClosure<T> : IHierarchicalEntityClosure where T : HierarchicalEntity<T>
{
    public Guid AncestorId { get; set; }
    public T Ancestor { get; set; } = default!;

    public Guid DescendantId { get; set; }
    public T Descendant { get; set; } = default!;
    public int Depth { get; set; }

    protected HierarchicalEntityClosure() { }
    // Use this entity for ancestor/descendant mapping. Construction and updates should be handled in the application layer when parent changes.
}