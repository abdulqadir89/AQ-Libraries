namespace AQ.Abstractions;


/// <summary>
/// Interface for entities that have a status.
/// </summary>
public interface IHasStatus<TStatus>
{
    /// <summary>
    /// Current status of the entity.
    /// </summary>
    TStatus Status { get; }

    /// <summary>
    /// Sets the status of the entity.
    /// </summary>
    /// <param name="status">The status to set.</param>
    void SetStatus(TStatus status);
}
