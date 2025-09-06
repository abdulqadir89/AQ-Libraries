namespace AQ.Domain.Entities;

public interface IMergeable<T> where T : IEntity
{
    Guid? PrimaryId { get; } // If null, it's the primary entity; if not, it's a merged/child of the primary
    bool IsPrimary => PrimaryId == null;
    void Merge(IEnumerable<T> childEntities);
}
