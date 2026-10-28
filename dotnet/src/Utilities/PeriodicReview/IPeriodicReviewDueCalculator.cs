namespace AQ.Utilities.PeriodicReview;

/// <summary>Works out the next review date of an entity when the caller gives none: a fixed interval, or a rule on the entity's own data. Returns null when there is no rule.</summary>
public interface IPeriodicReviewDueCalculator<in TEntity> where TEntity : IPeriodicReviewable
{
    ValueTask<DateTimeOffset?> CalculateAsync(TEntity entity, DateTimeOffset reviewedAt, CancellationToken cancellationToken = default);
}
