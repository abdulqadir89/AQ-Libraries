using AQ.Utilities.Verification;

namespace AQ.Utilities.Review;

public sealed class ReviewState
{
    public DateTimeOffset? NextReviewDue { get; private set; }
    public DateTimeOffset? LastReviewedAt { get; private set; }
    public Guid? LastReviewedById { get; private set; }
    public Guid? LastReviewActivityId { get; private set; }
    public DateTimeOffset? LastNotifiedAt { get; private set; }

    public void MarkReviewed(Guid reviewedByUserId, DateTimeOffset? nextReviewDue, Guid? activityId = null)
    {
        var now = DateTimeOffset.UtcNow;
        LastReviewedAt = now;
        LastReviewedById = reviewedByUserId;
        LastReviewActivityId = activityId;
        NextReviewDue = nextReviewDue ?? (NextReviewDue > now ? NextReviewDue : null);
        LastNotifiedAt = null;
    }

    public void Restamp() => LastReviewedAt = DateTimeOffset.UtcNow;

    public void SetNextReviewDue(DateTimeOffset? nextReviewDue)
    {
        if (NextReviewDue == nextReviewDue)
            return;

        NextReviewDue = nextReviewDue;
        LastNotifiedAt = null;
    }

    public void RecordNotified() => LastNotifiedAt = DateTimeOffset.UtcNow;

    public void Clear()
    {
        NextReviewDue = null;
        LastReviewedAt = null;
        LastReviewedById = null;
        LastReviewActivityId = null;
        LastNotifiedAt = null;
    }

    public bool IsDue(DateTimeOffset now) =>
        NextReviewDue is { } due && due <= now && (LastNotifiedAt is null || LastNotifiedAt < due);

    public bool IsCurrent(VerificationState? verification = null) =>
        LastReviewedAt is { } reviewedAt && (verification?.LastResetAt is not { } resetAt || reviewedAt >= resetAt);
}
