namespace AQ.Utilities.Review;

public interface IReviewable
{
    ReviewStateBase Review { get; }
}

/// <summary>An entity declares its concrete review state type: <see cref="ReviewState"/>, or the app's own type deriving from <see cref="ReviewStateBase"/>.</summary>
public interface IReviewable<out TState> : IReviewable where TState : ReviewStateBase
{
    new TState Review { get; }

    ReviewStateBase IReviewable.Review => Review;
}
