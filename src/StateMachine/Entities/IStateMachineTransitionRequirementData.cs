namespace AQ.StateMachine.Entities;

/// <summary>
/// Marker interface for entities that provide data required by state machine transition requirements.
/// Implementing this interface signals that this entity type contains user-provided data
/// that must be collected before a transition can be evaluated.
/// Handlers will fetch this data directly from the database using StateMachineId and TransitionId.
/// </summary>
public interface IStateMachineTransitionRequirementData
{
    /// <summary>
    /// The ID of the state machine instance this data is associated with.
    /// </summary>
    Guid StateMachineId { get; }

    /// <summary>
    /// The ID of the transition this data is for.
    /// </summary>
    Guid TransitionId { get; }
}
