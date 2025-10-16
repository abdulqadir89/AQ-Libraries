namespace AQ.StateMachine.Entities;

/// <summary>
/// Base interface for all state machine transition effects.
/// Effects are executed after a successful transition and are stored as JSON in the database.
/// The effect type is determined by the actual implementation class type.
/// </summary>
public interface IStateMachineTransitionEffect
{
    // No properties needed - type is determined by actual implementation type
}

/// <summary>
/// Base abstract class for transition effects with common properties.
/// The effect type is automatically determined by the implementing class type.
/// </summary>
public abstract record StateMachineTransitionEffect : IStateMachineTransitionEffect
{
    public string? Description { get; init; }
    public bool IsOptional { get; init; } = false;
    public int ExecutionOrder { get; init; } = 0;

    /// <summary>
    /// Gets the effect type name based on the actual implementation type.
    /// </summary>
    public virtual string GetEffectTypeName() => GetType().Name;
}

/// <summary>
/// Information about a completed transition for effect handlers.
/// </summary>
public class TransitionExecutionInfo
{
    public Guid StateMachineId { get; set; }
    public Guid? PreviousStateId { get; set; }
    public Guid NewStateId { get; set; }
    public Guid? TriggerId { get; set; }
    public bool WasForced { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset TransitionedAt { get; set; }
}

/// <summary>
/// Handler interface for specific effect types.
/// Multiple handlers can exist for the same effect type (all will be executed).
/// Handlers are responsible for fetching their own data from the database using the state machine ID.
/// </summary>
public interface IStateMachineTransitionEffectHandler<TEffect> where TEffect : IStateMachineTransitionEffect
{
    /// <summary>
    /// Executes the effect after a successful transition.
    /// Handler should fetch state machine instance and related data from database as needed.
    /// </summary>
    /// <param name="effect">The effect to execute</param>
    /// <param name="stateMachineId">The state machine instance ID</param>
    /// <param name="transitionInfo">Information about the completed transition</param>
    /// <returns>True if effect executed successfully, false otherwise</returns>
    Task<bool> HandleAsync(TEffect effect, Guid stateMachineId, TransitionExecutionInfo transitionInfo);
}

/// <summary>
/// Generic handler interface that can handle all effects.
/// This handler receives all effects with their execution status and can process them as needed.
/// Handler is responsible for fetching its own data from the database using the state machine ID.
/// </summary>
public interface IStateMachineTransitionEffectHandler
{
    /// <summary>
    /// Handles all effects for a transition. Called after specific handlers have been executed.
    /// Handler should fetch state machine instance and related data from database as needed.
    /// </summary>
    /// <param name="effects">All effects with their current execution status</param>
    /// <param name="stateMachineId">The state machine instance ID</param>
    /// <param name="transitionInfo">Information about the completed transition</param>
    /// <returns>Updated effects with final execution status</returns>
    Task<IEnumerable<EffectExecutionStatus>> HandleAsync(
        IEnumerable<EffectExecutionStatus> effects,
        Guid stateMachineId,
        TransitionExecutionInfo transitionInfo);
}

/// <summary>
/// Represents the execution status of a single effect.
/// </summary>
public class EffectExecutionStatus
{
    public IStateMachineTransitionEffect Effect { get; set; } = default!;
    public bool IsExecuted { get; set; }
    public string? FailureReason { get; set; }
    public string? HandlerUsed { get; set; }
    public bool WasProcessedBySpecificHandler { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
    public TimeSpan? ExecutionDuration { get; set; }
}

