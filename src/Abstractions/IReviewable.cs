namespace AQ.Abstractions;

/// <summary>
/// Represents an entity that can be reviewed.
/// </summary>
public interface IReviewable
{
    /// <summary>
    /// Gets the date and time when the entity was last reviewed.
    /// </summary>
    DateTimeOffset? LastReviewedAt { get; }

    /// <summary>
    /// Gets the ID of the user who last reviewed the entity.
    /// </summary>
    Guid? LastReviewedById { get; }

    /// <summary>
    /// Gets the date when the next review is scheduled.
    /// </summary>
    DateOnly? NextReviewDate { get; }

    /// <summary>
    /// Gets the review comments (supports markdown).
    /// </summary>
    string? ReviewComments { get; }
}
