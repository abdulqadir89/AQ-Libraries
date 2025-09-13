namespace AQ.Abstractions;

public interface IHierarchicalEntity : IEntity
{
    Guid? ParentId { get; }
    int Level { get; }
}
