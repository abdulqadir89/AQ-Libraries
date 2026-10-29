using AQ.Entities;

namespace AQ.Utilities.Commenting;

public interface IComment : IEntity
{
    /// <summary>Null for a top-level comment; a reply holds the id of the comment it answers.</summary>
    Guid? ParentId { get; }

    /// <summary>0 for a top-level comment, the depth of the comment it answers plus one for a reply.</summary>
    int Depth { get; }

    string Text { get; }
    DateTimeOffset? EditedAt { get; }
    DateTimeOffset? DeletedAt { get; }
    bool IsDeleted { get; }
}
