using AQ.Entities;

namespace AQ.Utilities.Tagging;

public abstract class EntityTag<TTag> : Entity, IEntityTag where TTag : class, ITag
{
    public Guid EntityId { get; protected set; }
    public Guid TagId { get; protected set; }
    public TTag? Tag { get; protected set; }

    protected EntityTag() { }
}
