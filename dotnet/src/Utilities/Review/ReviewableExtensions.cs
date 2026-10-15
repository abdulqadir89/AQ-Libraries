using AQ.Utilities.Verification;

namespace AQ.Utilities.Review;

public static class ReviewableExtensions
{
    // Returns true when the review also added a verification entry, so the caller can write the verification history row.
    // RULE RVW-2
    public static bool MarkReviewed<T>(this T entity, Guid reviewedByUserId, DateTimeOffset? nextReviewDue, Guid? activityId = null)
        where T : IReviewable
    {
        entity.Review.MarkReviewed(reviewedByUserId, nextReviewDue, activityId);

        if (entity is IVerifiable verifiable && !verifiable.Verification.HasBeenVerifiedBy(reviewedByUserId))
        {
            verifiable.Verification.Add(reviewedByUserId);
            return true;
        }

        return false;
    }

    public static bool IsReviewCurrent<T>(this T entity) where T : IReviewable =>
        entity.Review.IsCurrent((entity as IVerifiable)?.Verification);
}
