namespace AQ.Utilities.Review;

public static class ReviewableExtensions
{
    /// <summary>Marks the entity reviewed. The next due date is the one given, else the calculator's, else a still-future date the entity already has.</summary>
    // RULE RVW-2
    public static async Task MarkReviewedAsync<T>(
        this T entity,
        IReviewDueCalculator<T> calculator,
        DateTimeOffset? reviewedAt = null,
        DateTimeOffset? nextReviewDue = null,
        CancellationToken cancellationToken = default)
        where T : IReviewable
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(calculator);

        var at = reviewedAt ?? DateTimeOffset.UtcNow;
        var due = nextReviewDue ?? await calculator.CalculateAsync(entity, at, cancellationToken);
        entity.Review.MarkReviewed(at, due);
    }
}
