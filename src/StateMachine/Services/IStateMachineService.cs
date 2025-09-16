using AQ.Abstractions;
using AQ.StateMachineEntities;
using AQ.Utilities.Results;

namespace AQ.StateMachine.Services;

/// <summary>
/// Service for handling state machine operations including transitions, requirements evaluation, and reverts.
/// </summary>
public interface IStateMachineService
{
    /// <summary>
    /// Gets valid transitions for the specified trigger, evaluating all requirements.
    /// This method does not execute the transition but validates it can be performed.
    /// </summary>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="trigger">The trigger to evaluate</param>
    /// <param name="requirementsContext">Requirements context where key is requirement type name and value is context object</param>
    /// <returns>Result with valid transitions that can be executed</returns>
    Task<Result<IEnumerable<ValidTransition>>> GetValidTransitionsAsync(
        StateMachineInstance stateMachine,
        StateMachineTrigger trigger,
        IDictionary<string, object>? requirementsContext = null);

    /// <summary>
    /// Executes effects for a completed transition.
    /// This method should be called after a transition has been executed on the state machine instance.
    /// </summary>
    /// <param name="transition">The transition that was executed</param>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="transitionInfo">Information about the completed transition</param>
    /// <returns>Result with effect execution summary</returns>
    Task<Result<EffectExecutionSummary?>> ExecuteEffectsAsync(
        StateMachineTransition transition,
        StateMachineInstance stateMachine,
        StateMachineTransitionInfo transitionInfo);

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
    async Task<Result<StateMachineRevertInfo>> RevertLastTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        string reason,
        TUser revertedBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId> => await RevertTransitionsAsync<TUser, TUserId>(stateMachine, 1, reason, revertedBy);
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
/// Information about a validated transition that can be executed.
/// </summary>
public class ValidTransition
{
    public StateMachineTransition Transition { get; set; } = default!;
    public RequirementEvaluationSummary RequirementEvaluation { get; set; } = default!;
    public bool CanExecute => RequirementEvaluation.AllRequirementsMet;
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
