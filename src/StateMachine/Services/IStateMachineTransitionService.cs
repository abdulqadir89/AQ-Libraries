using AQ.Abstractions;
using AQ.StateMachineEntities;
using AQ.Utilities.Results;

namespace AQ.StateMachine.Services;

/// <summary>
/// Service for handling state machine transitions with requirement evaluation.
/// </summary>
public interface IStateMachineTransitionService
{
    /// <summary>
    /// Attempts to transition a state machine using the specified trigger.
    /// Evaluates all transition requirements before allowing the transition.
    /// </summary>
    /// <typeparam name="TUser">User type</typeparam>
    /// <typeparam name="TUserId">User ID type</typeparam>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="trigger">The trigger to execute</param>
    /// <param name="triggeredBy">The user triggering the transition</param>
    /// <param name="requirementsContext">Requirements context where key is requirement type name and value is context object</param>
    /// <returns>Result indicating success or failure with details</returns>
    Task<Result<StateMachineTransitionInfo>> TryTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        StateMachineTrigger trigger,
        TUser triggeredBy,
        IDictionary<string, object>? requirementsContext = null)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>;

    /// <summary>
    /// Attempts to transition a state machine using the specified trigger name.
    /// </summary>
    /// <typeparam name="TUser">User type</typeparam>
    /// <typeparam name="TUserId">User ID type</typeparam>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="triggerName">The name of the trigger to execute</param>
    /// <param name="triggeredBy">The user triggering the transition</param>
    /// <param name="requirementsContext">Requirements context where key is requirement type name and value is context object</param>
    /// <returns>Result indicating success or failure with details</returns>
    Task<Result<StateMachineTransitionInfo>> TryTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        string triggerName,
        TUser triggeredBy,
        IDictionary<string, object>? requirementsContext = null)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>;

    /// <summary>
    /// Forces a transition to a new state without requirement validation.
    /// Use with caution as this bypasses all business rules.
    /// </summary>
    /// <typeparam name="TUser">User type</typeparam>
    /// <typeparam name="TUserId">User ID type</typeparam>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="targetState">The target state entity</param>
    /// <param name="reason">Reason for the forced transition</param>
    /// <param name="triggeredBy">The user performing the forced transition</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<StateMachineTransitionInfo>> ForceTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        StateMachineState targetState,
        string reason,
        TUser triggeredBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>;

    /// <summary>
    /// Evaluates all requirements for a specific transition without executing it.
    /// Useful for UI validation and preview purposes.
    /// </summary>
    /// <param name="transition">The transition to evaluate</param>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="requirementsContext">Requirements context where key is requirement type name and value is context object</param>
    /// <returns>Result with requirement evaluation details</returns>
    Task<Result<RequirementEvaluationSummary>> EvaluateRequirementsAsync(
        StateMachineTransition transition,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null);

    /// <summary>
    /// Gets all available transitions from the current state with requirement evaluation status.
    /// </summary>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="requirementsContext">Requirements context where key is requirement type name and value is context object</param>
    /// <returns>List of available transitions with their requirement status</returns>
    Task<IEnumerable<AvailableTransition>> GetAvailableTransitionsAsync(
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null);

    /// <summary>
    /// Reverts the specified number of transitions, moving the state machine back in its history.
    /// This operation marks the reverted transitions with RevertedAt timestamp.
    /// </summary>
    /// <typeparam name="TUser">User type</typeparam>
    /// <typeparam name="TUserId">User ID type</typeparam>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="numberOfTransitions">Number of transitions to revert (must be positive)</param>
    /// <param name="reason">Reason for the revert operation</param>
    /// <param name="revertedBy">The user performing the revert</param>
    /// <returns>Result with revert operation details</returns>
    Task<Result<StateMachineRevertInfo>> RevertTransitionsAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        int numberOfTransitions,
        string reason,
        TUser revertedBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>;

    /// <summary>
    /// Reverts the last transition, moving the state machine back one step in its history.
    /// This operation marks the last non-reverted transition with RevertedAt timestamp.
    /// </summary>
    /// <typeparam name="TUser">User type</typeparam>
    /// <typeparam name="TUserId">User ID type</typeparam>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="reason">Reason for the revert operation</param>
    /// <param name="revertedBy">The user performing the revert</param>
    /// <returns>Result with revert operation details</returns>
    Task<Result<StateMachineRevertInfo>> RevertLastTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        string reason,
        TUser revertedBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>;
}

/// <summary>
/// Information about a state machine transition operation.
/// </summary>
public class StateMachineTransitionInfo
{
    public bool Success { get; set; }
    public Guid? PreviousStateId { get; set; }
    public Guid? NewStateId { get; set; }
    public Guid? TriggerId { get; set; }
    public bool WasForced { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset TransitionedAt { get; set; }
    public RequirementEvaluationSummary? RequirementEvaluation { get; set; }
    public EffectExecutionSummary? EffectExecution { get; set; }
}

/// <summary>
/// Summary of requirement evaluation for a transition.
/// </summary>
public class RequirementEvaluationSummary
{
    public bool AllRequirementsMet { get; set; }
    public IEnumerable<RequirementEvaluationStatus> RequirementResults { get; set; } = [];
    public IEnumerable<string> FailureReasons { get; set; } = [];
}

/// <summary>
/// Information about an available transition with requirement status.
/// </summary>
public class AvailableTransition
{
    public Guid TriggerId { get; set; }
    public string TriggerName { get; set; } = default!;
    public Guid? ToStateId { get; set; }
    public string? ToStateName { get; set; }
    public Guid TransitionId { get; set; }
    public bool CanExecute { get; set; }
    public RequirementEvaluationSummary? RequirementEvaluation { get; set; }
}

/// <summary>
/// Information about a state machine revert operation.
/// </summary>
public class StateMachineRevertInfo
{
    public bool Success { get; set; }
    public Guid PreviousStateId { get; set; }
    public Guid NewStateId { get; set; }
    public int TransitionsReverted { get; set; }
    public string Reason { get; set; } = default!;
    public DateTimeOffset RevertedAt { get; set; }
    public IEnumerable<StateMachineStateTransitionHistory> MarkedAsRevertedTransitions { get; set; } = [];
}
