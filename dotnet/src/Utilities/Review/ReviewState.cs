namespace AQ.Utilities.Review;

// ADR-005: a review is only a schedule (two dates), not who/how/notified, and it is independent of verification
/// <summary>The schedule of a review: when the entity was last reviewed and when it is next due. An app that needs more columns derives from this and maps its type with <c>HasReviewState</c>.</summary>
public abstract class ReviewStateBase
{
    public DateTimeOffset? LastReviewedAt { get; private set; }
    public DateTimeOffset? NextReviewDue { get; private set; }

    // RULE RVW-1
    public void MarkReviewed(DateTimeOffset reviewedAt, DateTimeOffset? nextReviewDue)
    {
        LastReviewedAt = reviewedAt;
        NextReviewDue = nextReviewDue ?? (NextReviewDue > reviewedAt ? NextReviewDue : null);
    }

    public void SetNextReviewDue(DateTimeOffset? nextReviewDue) => NextReviewDue = nextReviewDue;

    public void Clear()
    {
        LastReviewedAt = null;
        NextReviewDue = null;
    }

    // RULE RVW-3
    public bool IsOverdue(DateTimeOffset now) => NextReviewDue is { } due && due <= now;
}

public sealed class ReviewState : ReviewStateBase;
