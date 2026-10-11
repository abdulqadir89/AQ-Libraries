namespace AQ.Entities;

public interface IHierarchicalEntityClosure
{
    Guid AncestorId { get; set; }
    Guid DescendantId { get; set; }
}
