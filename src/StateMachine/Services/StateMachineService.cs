using AQ.Abstractions;
using AQ.StateMachineEntities;
using AQ.Utilities.Results;

namespace AQ.StateMachine.Services;

/// <summary>
/// Implementation of state machine service for handling operations including transitions, requirements evaluation, and reverts.
/// This service acts as a higher-level orchestrator that doesn't handle persistence directly.
/// </summary>
public class StateMachineService : IStateMachineService
{
    private readonly IStateMachineRequirementEvaluationService _requirementEvaluationService;
    private readonly IStateMachineEffectExecutionService? _effectExecutionService;

    public StateMachineService(
        IStateMachineRequirementEvaluationService requirementEvaluationService,
        IStateMachineEffectExecutionService? effectExecutionService = null)
    {
        _requirementEvaluationService = requirementEvaluationService ?? throw new ArgumentNullException(nameof(requirementEvaluationService));
        _effectExecutionService = effectExecutionService;
    }

    public async Task<Result<ValidTransition>> GetFirstValidTransitionAsync(
        StateMachineInstance stateMachine,
        StateMachineTrigger trigger,
        IDictionary<string, object>? requirementsContext = null)
    {
        if (stateMachine == null)
            return Result.Failure<ValidTransition>("State machine instance cannot be null");

        if (trigger == null)
            return Result.Failure<ValidTransition>("Trigger cannot be null");

        try
        {
            var transitions = stateMachine.GetTransitionsForTrigger(trigger);

            foreach (var transition in transitions)
            {
                var evaluationResult = await EvaluateRequirementsAsync(transition, stateMachine, requirementsContext);

                if (evaluationResult.IsSuccess && evaluationResult.Value.AllRequirementsMet)
                {
                    return Result.Success(new ValidTransition
                    {
                        Transition = transition,
                        RequirementEvaluation = evaluationResult.Value
                    });
                }
            }

            // No valid transitions found
            return Result.Failure<ValidTransition>("No valid transitions found for the given trigger");
        }
        catch (Exception ex)
        {
            return Result.Failure<ValidTransition>($"Failed to get first valid transition: {ex.Message}");
        }
    }

    public async Task<Result<EffectExecutionSummary?>> ExecuteEffectsAsync(
        StateMachineTransition transition,
        StateMachineInstance stateMachine,
        StateMachineTransitionInfo transitionInfo)
    {
        if (transition == null)
            return Result.Failure<EffectExecutionSummary?>("Transition cannot be null");

        if (stateMachine == null)
            return Result.Failure<EffectExecutionSummary?>("State machine instance cannot be null");

        if (transitionInfo == null)
            return Result.Failure<EffectExecutionSummary?>("Transition info cannot be null");

        try
        {
            // If no effect execution service or no effects, return empty result
            if (_effectExecutionService == null || !transition.HasEffects)
            {
                return Result.Success<EffectExecutionSummary?>(null);
            }

            var effectExecutionResult = await _effectExecutionService.ExecuteEffectsAsync(
                transition.Effects!, stateMachine, transitionInfo);

            return Result.Success<EffectExecutionSummary?>(effectExecutionResult);
        }
        catch (Exception ex)
        {
            return Result.Failure<EffectExecutionSummary?>($"Failed to execute effects: {ex.Message}");
        }
    }

    public async Task<Result<RequirementEvaluationSummary>> EvaluateRequirementsAsync(
        StateMachineTransition transition,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null)
    {
        if (transition == null)
            return Result.Failure<RequirementEvaluationSummary>("Transition cannot be null");

        if (stateMachine == null)
            return Result.Failure<RequirementEvaluationSummary>("State machine instance cannot be null");

        try
        {
            if (!transition.HasRequirements)
            {
                return Result.Success(new RequirementEvaluationSummary
                {
                    AllRequirementsMet = true,
                    RequirementResults = [],
                    FailureReasons = []
                });
            }

            var evaluationResult = await _requirementEvaluationService.EvaluateRequirementsAsync(
                transition.Requirements!, stateMachine, requirementsContext);

            return Result.Success(evaluationResult);
        }
        catch (Exception ex)
        {
            return Result.Failure<RequirementEvaluationSummary>($"Failed to evaluate requirements: {ex.Message}");
        }
    }

    public async Task<IEnumerable<AvailableTransition>> GetAvailableTransitionsAsync(
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null)
    {
        if (stateMachine == null)
            return [];

        try
        {
            var availableTransitions = new List<AvailableTransition>();
            var transitions = stateMachine.GetAvailableTransitions();

            foreach (var transition in transitions)
            {
                var evaluationResult = await EvaluateRequirementsAsync(transition, stateMachine, requirementsContext);

                availableTransitions.Add(new AvailableTransition
                {
                    TriggerId = transition.TriggerId,
                    TriggerName = transition.Trigger.Name,
                    ToStateId = transition.ToStateId,
                    ToStateName = transition.ToState?.Name,
                    TransitionId = transition.Id,
                    CanExecute = evaluationResult.IsSuccess && evaluationResult.Value.AllRequirementsMet,
                    RequirementEvaluation = evaluationResult.IsSuccess ? evaluationResult.Value : null
                });
            }

            return availableTransitions;
        }
        catch (Exception)
        {
            return [];
        }
    }

    public Task<Result<StateMachineRevertInfo>> RevertTransitionsAsync<TUser, TUserId>(
        StateMachineInstance stateMachine,
        int numberOfTransitions,
        string reason,
        TUser revertedBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        if (stateMachine == null)
            return Task.FromResult(Result.Failure<StateMachineRevertInfo>("State machine instance cannot be null"));

        if (numberOfTransitions <= 0)
            return Task.FromResult(Result.Failure<StateMachineRevertInfo>("Number of transitions to revert must be positive"));

        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(Result.Failure<StateMachineRevertInfo>("Reason for revert cannot be empty"));

        if (revertedBy == null)
            return Task.FromResult(Result.Failure<StateMachineRevertInfo>("Reverted by user cannot be null"));

        try
        {
            var previousStateId = stateMachine.CurrentStateId;

            // Get non-reverted transitions in reverse chronological order
            var transitionsToRevert = stateMachine.TransitionHistory
                .Where(h => !h.IsReverted)
                .OrderByDescending(h => h.TransitionedAt)
                .Take(numberOfTransitions)
                .ToList();

            if (transitionsToRevert.Count == 0)
            {
                return Task.FromResult(Result.Failure<StateMachineRevertInfo>("No transitions available to revert"));
            }

            if (transitionsToRevert.Count < numberOfTransitions)
            {
                return Task.FromResult(Result.Failure<StateMachineRevertInfo>($"Only {transitionsToRevert.Count} transitions available to revert, but {numberOfTransitions} requested"));
            }

            // Find the target state (the from state of the oldest transition we're reverting)
            var oldestTransitionToRevert = transitionsToRevert.Last(); // Last in reverse order is oldest
            var targetState = oldestTransitionToRevert.FromState;

            // Execute the revert on the state machine instance
            stateMachine.ExecuteRevert<TUser, TUserId>(targetState, transitionsToRevert);

            var revertInfo = new StateMachineRevertInfo
            {
                Success = true,
                PreviousStateId = previousStateId,
                NewStateId = stateMachine.CurrentStateId,
                TransitionsReverted = transitionsToRevert.Count,
                Reason = reason,
                RevertedAt = DateTimeOffset.UtcNow,
                MarkedAsRevertedTransitions = transitionsToRevert
            };

            return Task.FromResult(Result.Success(revertInfo));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<StateMachineRevertInfo>($"Failed to revert transitions: {ex.Message}"));
        }
    }
}