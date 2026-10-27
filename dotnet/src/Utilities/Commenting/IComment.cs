using AQ.Entities;

namespace AQ.Utilities.Commenting;

public interface IComment : IEntity
{
    /// <summary>Null for a top-level comment; a reply holds the id of the top-level comment it answers.</summary>
    Guid? ParentId { get; }

    string Text { get; }
    DateTimeOffset? EditedAt { get; }
    DateTimeOffset? DeletedAt { get; }
    bool IsDeleted { get; }
}
