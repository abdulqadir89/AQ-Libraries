namespace AQ.Utilities.PeriodicReview;

public interface IPeriodicReviewable
{
    PeriodicReviewStateBase PeriodicReview { get; }
}

/// <summary>An entity declares its concrete review state type: <see cref="PeriodicReviewState"/>, or the app's own type deriving from <see cref="PeriodicReviewStateBase"/>.</summary>
public interface IPeriodicReviewable<out TState> : IPeriodicReviewable where TState : PeriodicReviewStateBase
{
    new TState PeriodicReview { get; }

    PeriodicReviewStateBase IPeriodicReviewable.PeriodicReview => PeriodicReview;
}
