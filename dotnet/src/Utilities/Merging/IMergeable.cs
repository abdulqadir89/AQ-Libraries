using AQ.Entities;

namespace AQ.Utilities.Merging;

public interface IMergeable<T> where T : IEntity, IMergeable<T>
{
    Guid? PrimaryId { get; } // If null, it's the primary entity; if not, it's a merged/child of the primary
    bool IsPrimary => PrimaryId == null;
    void SetPrimaryId(Guid? primaryId);
}
