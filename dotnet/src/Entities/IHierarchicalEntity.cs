namespace AQ.Entities;

public interface IHierarchicalEntity : IEntity
{
    Guid? ParentId { get; }
}
