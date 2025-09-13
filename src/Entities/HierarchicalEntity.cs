using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using AQ.Abstractions;

namespace AQ.Entities;

/// <summary>
/// Abstract base class for entities that form hierarchical structures.
/// Provides common functionality for self-referential parent-child relationships.
/// </summary>
/// <typeparam name="T">The concrete entity type implementing the hierarchy</typeparam>
public abstract class HierarchicalEntity<T> : Entity, IHierarchicalEntity where T : HierarchicalEntity<T>
{
    public int Level { get; protected set; }

    // Self-referential for n-level hierarchy
    public Guid? ParentId { get; protected set; }
    public T? Parent { get; protected set; }
    public ICollection<T> Children { get; protected set; } = default!;

    [InverseProperty(nameof(HierarchicalEntityClosure<T>.Descendant))]
    public ICollection<HierarchicalEntityClosure<T>> Ancestors { get; set; } = default!;

    [InverseProperty(nameof(HierarchicalEntityClosure<T>.Ancestor))]
    public ICollection<HierarchicalEntityClosure<T>> Descendants { get; set; } = default!;

    protected HierarchicalEntity() { }

    public bool IsRoot => ParentId is null;

    public bool IsLeaf => !Children.Any();

    public async Task SetParent(T? parent, Func<T, Guid?, Task> updateClosure)
    {
        await updateClosure.Invoke((T)this, parent?.Id);

        Parent = parent;
        ParentId = parent?.Id;
        Level = parent is null ? 0 : parent.Level + 1;
    }

}
