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
        var availableTransitions = stateMachine.Definition.Transitions
            .Where(t => t.FromStateId == stateMachine.CurrentStateId && t.Trigger.Id == trigger.Id)
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

        var transitionsFromCurrentState = stateMachine.Definition.Transitions
            .Where(t => t.FromStateId == stateMachine.CurrentStateId);

        foreach (var transition in transitionsFromCurrentState)
        {
            var evaluationResult = await EvaluateRequirementsAsync(transition, stateMachine, requirementsContext);

            availableTransitions.Add(new AvailableTransition
            {
                TriggerId = transition.Trigger.Id,
                TriggerName = transition.Trigger.Name,
                ToStateId = transition.ToState.Id,
                ToStateName = transition.ToState.Name,
                TransitionId = transition.Id,
                CanExecute = evaluationResult.IsSuccess && evaluationResult.Value.AllRequirementsMet,
                RequirementEvaluation = evaluationResult.IsSuccess ? evaluationResult.Value : null
            });
        }

        return availableTransitions;
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
