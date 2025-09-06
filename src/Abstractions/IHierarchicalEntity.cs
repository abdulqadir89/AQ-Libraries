namespace AQ.Abstractions;

public interface IHierarchicalEntity : IEntity
{
    /// <summary>
    /// The hierarchy path of this entity (e.g., "/engineering/software")
    /// </summary>
    string HierarchyPath { get; }
}
