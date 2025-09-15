namespace AQ.Abstractions;

public interface IHierarchicalEntity : IEntity
{
    Guid? ParentId { get; }
}
