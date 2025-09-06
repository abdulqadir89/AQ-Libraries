namespace AQ.Domain.Entities;

/// <summary>
/// Interface for entities that can be activated/deactivated.
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Indicates whether the entity is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Sets the active state of the entity.
    /// </summary>
    /// <param name="isActive">The active state to set.</param>
    void SetActiveState(bool isActive);
}
