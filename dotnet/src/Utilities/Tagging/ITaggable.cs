using AQ.Entities;

namespace AQ.Utilities.Tagging;

public interface ITaggable : IEntity
{
    IReadOnlyCollection<Guid> TagIds { get; }
    void AddTagId(Guid tagId);
    void RemoveTagId(Guid tagId);
}
