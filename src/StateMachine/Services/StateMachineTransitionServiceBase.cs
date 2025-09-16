using AQ.Abstractions;
using AQ.StateMachineEntities;
using AQ.Utilities.Results;

namespace AQ.StateMachine.Services;

/// <summary>
/// Base implementation of the state machine transition service.
/// This is a skeleton implementation that shows the structure.
/// Concrete implementations should be provided in the Core application layer.
/// </summary>
public abstract class StateMachineTransitionServiceBase : IStateMachineTransitionService
{
    protected readonly IStateMachineRequirementEvaluationService _requirementEvaluationService;
    protected readonly IStateMachineEffectExecutionService? _effectExecutionService;

    protected StateMachineTransitionServiceBase(
        IStateMachineRequirementEvaluationService requirementEvaluationService,
        IStateMachineEffectExecutionService? effectExecutionService = null)
    {
        _requirementEvaluationService = requirementEvaluationService ?? throw new ArgumentNullException(nameof(requirementEvaluationService));
        _effectExecutionService = effectExecutionService;
    }

    public async Task<Result<StateMachineTransitionInfo>> TryTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        StateMachineTrigger trigger,
        TUser triggeredBy,
        IDictionary<string, object>? requirementsContext = null)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        // 1. Find available transitions for the trigger
        // Include both state-changing transitions and trigger-only transitions
        var availableTransitions = stateMachine.Definition.Transitions
            .Where(t => t.Trigger.Id == trigger.Id &&
                       (t.FromStateId == stateMachine.CurrentStateId || t.FromStateId == null))
            .ToList();

        if (!availableTransitions.Any())
        {
            return Result.Failure<StateMachineTransitionInfo>(
                new Error(ErrorType.Validation, "Transition.NoAvailableTransitions",
                    $"No transitions available from state '{stateMachine.CurrentState.Name}' with trigger '{trigger.Name}'"));
        }

        // 2. Evaluate each transition's requirements
        foreach (var transition in availableTransitions)
        {
            if (transition.Requirements?.Any() == true)
            {
                var evaluationResult = await _requirementEvaluationService.EvaluateRequirementsAsync(
                    transition.Requirements, stateMachine, requirementsContext);

                if (evaluationResult.AllRequirementsMet)
                {
                    // 3. Execute the transition
                    return await ExecuteTransitionAsync<TUser, TUserId>(stateMachine, transition, triggeredBy, evaluationResult);
                }
            }
            else
            {
                // No requirements, transition can proceed
                return await ExecuteTransitionAsync<TUser, TUserId>(stateMachine, transition, triggeredBy, null);
            }
        }

        // 4. No valid transition found
        return Result.Failure<StateMachineTransitionInfo>(
            new Error(ErrorType.Validation, "Transition.RequirementsNotMet",
                "No transition could be completed as requirements were not met"));
    }

    public async Task<Result<StateMachineTransitionInfo>> TryTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        string triggerName,
        TUser triggeredBy,
        IDictionary<string, object>? requirementsContext = null)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        var trigger = stateMachine.Definition.Triggers.FirstOrDefault(t => t.Name == triggerName);
        if (trigger == null)
        {
            return Result.Failure<StateMachineTransitionInfo>(
                new Error(ErrorType.Validation, "Trigger.NotFound",
                    $"Trigger '{triggerName}' not found in state machine definition"));
        }

        return await TryTransitionAsync<TUser, TUserId>(stateMachine, trigger, triggeredBy, requirementsContext);
    }

    public async Task<Result<StateMachineTransitionInfo>> ForceTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        StateMachineState targetState,
        string reason,
        TUser triggeredBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        if (targetState == null)
        {
            return Result.Failure<StateMachineTransitionInfo>(
                new Error(ErrorType.Validation, "State.Null",
                    "Target state cannot be null"));
        }

        // Verify the state belongs to this definition
        if (!stateMachine.Definition.States.Any(s => s.Id == targetState.Id))
        {
            return Result.Failure<StateMachineTransitionInfo>(
                new Error(ErrorType.Validation, "State.NotInDefinition",
                    $"State '{targetState.Name}' does not belong to this state machine definition"));
        }

        // Execute forced transition
        var previousState = stateMachine.CurrentState;
        stateMachine.ExecuteForcedTransition<TUser, TUserId>(targetState, reason, triggeredBy);

        // Save changes (implementation-specific)
        await SaveStateMachineChangesAsync(stateMachine);

        return Result.Success(new StateMachineTransitionInfo
        {
            Success = true,
            PreviousStateId = previousState.Id,
            NewStateId = targetState.Id,
            WasForced = true,
            Reason = reason,
            TransitionedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task<Result<RequirementEvaluationSummary>> EvaluateRequirementsAsync(
        StateMachineTransition transition,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null)
    {
        if (transition.Requirements?.Any() != true)
        {
            return Result.Success(new RequirementEvaluationSummary
            {
                AllRequirementsMet = true,
                RequirementResults = [],
                FailureReasons = []
            });
        }

        var evaluationResult = await _requirementEvaluationService.EvaluateRequirementsAsync(
            transition.Requirements, stateMachine, requirementsContext);

        return Result.Success(evaluationResult);
    }

    public async Task<IEnumerable<AvailableTransition>> GetAvailableTransitionsAsync(
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null)
    {
        var availableTransitions = new List<AvailableTransition>();

        // Include both state-changing transitions and trigger-only transitions
        var transitionsFromCurrentState = stateMachine.Definition.Transitions
            .Where(t => t.FromStateId == stateMachine.CurrentStateId || t.FromStateId == null);

        foreach (var transition in transitionsFromCurrentState)
        {
            var evaluationResult = await EvaluateRequirementsAsync(transition, stateMachine, requirementsContext);

            availableTransitions.Add(new AvailableTransition
            {
                TriggerId = transition.Trigger.Id,
                TriggerName = transition.Trigger.Name,
                ToStateId = transition.ToState?.Id,
                ToStateName = transition.ToState?.Name,
                TransitionId = transition.Id,
                CanExecute = evaluationResult.IsSuccess && evaluationResult.Value.AllRequirementsMet,
                RequirementEvaluation = evaluationResult.IsSuccess ? evaluationResult.Value : null
            });
        }

        return availableTransitions;
    }

    public async Task<Result<StateMachineRevertInfo>> RevertTransitionsAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        int numberOfTransitions,
        string reason,
        TUser revertedBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        // Validate input parameters
        if (numberOfTransitions <= 0)
        {
            return Result.Failure<StateMachineRevertInfo>(
                new Error(ErrorType.Validation, "Revert.InvalidCount",
                    "Number of transitions to revert must be greater than 0"));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<StateMachineRevertInfo>(
                new Error(ErrorType.Validation, "Revert.MissingReason",
                    "A reason must be provided for the revert operation"));
        }

        // Get only non-reverted transition history in chronological order (oldest first)
        var nonRevertedHistory = stateMachine.TransitionHistory
            .Where(h => !h.IsReverted)
            .OrderBy(h => h.TransitionedAt)
            .ToList();

        // Validate we have enough non-reverted transitions to revert
        if (nonRevertedHistory.Count < numberOfTransitions)
        {
            return Result.Failure<StateMachineRevertInfo>(
                new Error(ErrorType.Validation, "Revert.InsufficientHistory",
                    $"Cannot revert {numberOfTransitions} transitions. Only {nonRevertedHistory.Count} non-reverted transitions exist"));
        }

        // Find the target state after reverting (using only non-reverted transitions)
        var targetStateResult = GetTargetStateAfterRevert(stateMachine, nonRevertedHistory, numberOfTransitions);
        if (targetStateResult.IsFailure)
        {
            return Result.Failure<StateMachineRevertInfo>(targetStateResult.Error);
        }

        var targetState = targetStateResult.Value;
        var previousState = stateMachine.CurrentState;

        // Get the last N non-reverted transitions to mark as reverted
        var transitionsToRevert = nonRevertedHistory.TakeLast(numberOfTransitions).ToList();

        // Execute the revert operation
        stateMachine.ExecuteRevert<TUser, TUserId>(targetState, transitionsToRevert);

        // Save changes (implementation-specific)
        await SaveStateMachineChangesAsync(stateMachine);

        return Result.Success(new StateMachineRevertInfo
        {
            Success = true,
            PreviousStateId = previousState.Id,
            NewStateId = targetState.Id,
            TransitionsReverted = numberOfTransitions,
            Reason = reason,
            RevertedAt = DateTimeOffset.UtcNow,
            MarkedAsRevertedTransitions = transitionsToRevert
        });
    }

    public async Task<Result<StateMachineRevertInfo>> RevertLastTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        string reason,
        TUser revertedBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        return await RevertTransitionsAsync<TUser, TUserId>(stateMachine, 1, reason, revertedBy);
    }

    /// <summary>
    /// Determines the target state after reverting the specified number of transitions.
    /// Only considers non-reverted transitions for the calculation.
    /// </summary>
    protected virtual Result<StateMachineState> GetTargetStateAfterRevert(
        StateMachineInstance stateMachine,
        IList<StateMachineStateTransitionHistory> nonRevertedHistory,
        int numberOfTransitions)
    {
        if (numberOfTransitions <= 0)
        {
            return Result.Failure<StateMachineState>(
                new Error(ErrorType.Validation, "Revert.InvalidCount",
                    "Number of transitions must be greater than 0"));
        }

        if (nonRevertedHistory.Count == 0)
        {
            return Result.Failure<StateMachineState>(
                new Error(ErrorType.Validation, "Revert.NoHistory",
                    "No non-reverted transition history available"));
        }

        // If we're reverting all non-reverted transitions, go back to the initial state
        if (numberOfTransitions >= nonRevertedHistory.Count)
        {
            var initialState = stateMachine.Definition.InitialState;
            if (initialState == null)
            {
                return Result.Failure<StateMachineState>(
                    new Error(ErrorType.Validation, "Revert.NoInitialState",
                        "State machine definition has no initial state"));
            }
            return Result.Success(initialState);
        }

        // Find the state we should be in after reverting N transitions
        // This is the "ToState" of the transition that will remain after revert
        var targetTransitionIndex = nonRevertedHistory.Count - numberOfTransitions - 1;
        var targetTransition = nonRevertedHistory[targetTransitionIndex];

        // The target state is the ToState of the transition that will remain after revert
        var targetState = stateMachine.Definition.States.FirstOrDefault(s => s.Id == targetTransition.ToStateId);
        
        if (targetState == null)
        {
            return Result.Failure<StateMachineState>(
                new Error(ErrorType.Validation, "Revert.StateNotFound",
                    $"Target state with ID {targetTransition.ToStateId} not found in definition"));
        }

        return Result.Success(targetState);
    }

    /// <summary>
    /// Validates if a revert operation can be performed safely.
    /// Only considers non-reverted transitions.
    /// </summary>
    protected virtual Result ValidateRevertOperation(
        StateMachineInstance stateMachine,
        int numberOfTransitions)
    {
        if (numberOfTransitions <= 0)
        {
            return Result.Failure(
                new Error(ErrorType.Validation, "Revert.InvalidCount",
                    "Number of transitions to revert must be greater than 0"));
        }

        var nonRevertedHistory = stateMachine.TransitionHistory
            .Where(h => !h.IsReverted)
            .OrderBy(h => h.TransitionedAt)
            .ToList();

        if (nonRevertedHistory.Count < numberOfTransitions)
        {
            return Result.Failure(
                new Error(ErrorType.Validation, "Revert.InsufficientHistory",
                    $"Cannot revert {numberOfTransitions} transitions. Only {nonRevertedHistory.Count} non-reverted transitions exist"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Executes the actual transition and saves changes.
    /// Implementation-specific method that should be overridden.
    /// </summary>
    protected virtual async Task<Result<StateMachineTransitionInfo>> ExecuteTransitionAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        StateMachineTransition transition,
        TUser triggeredBy,
        RequirementEvaluationSummary? evaluationResult)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        var previousState = stateMachine.CurrentState;

        // Execute the transition using the internal method
        stateMachine.ExecuteTransition<TUser, TUserId>(transition, triggeredBy);

        // Save changes (implementation-specific)
        await SaveStateMachineChangesAsync(stateMachine);

        var transitionInfo = new StateMachineTransitionInfo
        {
            Success = true,
            PreviousStateId = previousState.Id,
            NewStateId = stateMachine.CurrentState.Id,
            TriggerId = transition.Trigger.Id,
            WasForced = false,
            TransitionedAt = DateTimeOffset.UtcNow,
            RequirementEvaluation = evaluationResult
        };

        // Execute effects if available
        EffectExecutionSummary? effectExecutionResult = null;
        if (_effectExecutionService != null && transition.HasEffects)
        {
            try
            {
                effectExecutionResult = await _effectExecutionService.ExecuteEffectsAsync(
                    transition.Effects!, stateMachine, transitionInfo);
                transitionInfo.EffectExecution = effectExecutionResult;
            }
            catch
            {
                // Effect execution failures don't prevent the transition from completing
                // In a real implementation, you'd use proper logging here
            }
        }

        return Result.Success(transitionInfo);
    }

    /// <summary>
    /// Saves state machine changes to persistence.
    /// Implementation-specific method that should be overridden.
    /// </summary>
    protected abstract Task SaveStateMachineChangesAsync(StateMachineInstance stateMachine);
}
