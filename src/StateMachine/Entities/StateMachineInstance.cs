using AQ.Abstractions;
using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// State machine instance that maintains current state and handles transitions.
/// Uses a state machine definition as a template and tracks transition history.
/// </summary>
public abstract class StateMachineInstance : Entity
{
    public Guid DefinitionId { get; protected set; }
    public StateMachineDefinition? Definition { get; protected set; }
    public StateMachineState? CurrentState { get; protected set; }
    public Guid CurrentStateId { get; protected set; }
    public DateTimeOffset? LastTransitionAt { get; protected set; }

    public ICollection<StateMachineStateTransitionHistory> TransitionHistory { get; protected set; } = [];

    // EF Core constructor
    protected StateMachineInstance() { }

    protected StateMachineInstance(
        StateMachineDefinition definition)
    {
        DefinitionId = definition?.Id ?? throw new ArgumentNullException(nameof(definition));
        Definition = definition;

        // Set current state to the definition's initial state
        var initialState = definition.InitialState ?? throw new InvalidOperationException("Definition must have an initial state.");
        CurrentState = initialState;
        CurrentStateId = initialState.Id;
    }


    /// <summary>
    /// Method for reverting to a specific state by marking transitions as reverted.
    /// Used by IStateMachineTransitionService.
    /// </summary>
    public void ExecuteRevert<TUser, TUserId>(
        StateMachineState targetState,
        IEnumerable<StateMachineStateTransitionHistory> transitionsToRevert)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        if (targetState == null)
            throw new ArgumentNullException(nameof(targetState));

        // Mark all specified transitions as reverted
        foreach (var transition in transitionsToRevert)
        {
            if (!transition.IsReverted)
            {
                transition.MarkAsReverted();
            }
        }

        // Update current state to the target state
        CurrentStateId = targetState.Id;
        CurrentState = targetState;
        LastTransitionAt = DateTimeOffset.UtcNow;

        // Raise domain event for revert operation if needed
    }

    /// <summary>
    /// Gets all available trigger entities from the current state.
    /// Includes both state-changing transitions and non-state-changing (trigger-only) transitions.
    /// </summary>
    public IEnumerable<StateMachineTrigger> GetAvailableTriggers()
    {
        // Final states have no outgoing transitions
        if (CurrentState?.Category == StateMachineStateCategory.Final)
            return [];

        return Definition!.Transitions
            .Where(t => t.FromStateId == CurrentStateId || t.FromStateId == null)
            .Select(t => t.Trigger!)
            .Distinct();
    }

    /// <summary>
    /// Gets all available transitions from the current state.
    /// </summary>
    public IEnumerable<StateMachineTransition> GetAvailableTransitions()
    {
        // Final states have no outgoing transitions
        if (CurrentState?.Category == StateMachineStateCategory.Final)
            return [];

        return Definition!.Transitions.Where(t => t.FromStateId == CurrentStateId || t.FromStateId == null);
    }

    /// <summary>
    /// Gets all requirements for a specific trigger from the current state.
    /// Includes requirements from both state-changing and non-state-changing transitions.
    /// </summary>
    public IEnumerable<IStateMachineTransitionRequirement> GetRequirementsForTrigger(StateMachineTrigger trigger)
    {
        if (trigger == null)
            return [];

        return Definition!.Transitions
            .Where(t => (t.FromStateId == CurrentStateId || t.FromStateId == null) && t.TriggerId == trigger.Id)
            .SelectMany(t => t.Requirements ?? [])
            .Distinct();
    }

    /// <summary>
    /// Gets all requirements for all available transitions from the current state.
    /// Includes requirements from both state-changing and non-state-changing transitions.
    /// </summary>
    public IEnumerable<IStateMachineTransitionRequirement> GetAllRequirementsFromCurrentState()
    {
        return Definition!.Transitions
            .Where(t => t.FromStateId == CurrentStateId || t.FromStateId == null)
            .SelectMany(t => t.Requirements ?? [])
            .Distinct();
    }

    /// <summary>
    /// Gets transitions that can be triggered with the specified trigger from the current state.
    /// Includes both state-changing transitions and non-state-changing (trigger-only) transitions.
    /// </summary>
    public IEnumerable<StateMachineTransition> GetTransitionsForTrigger(StateMachineTrigger trigger)
    {
        if (trigger == null)
            return [];

        return Definition!.Transitions.Where(t =>
            (t.FromStateId == CurrentStateId || t.FromStateId == null) && t.TriggerId == trigger.Id);
    }

    /// <summary>
    /// Checks if the state machine is in a final state.
    /// </summary>
    public bool IsInFinalState()
    {
        return CurrentState!.Category == StateMachineStateCategory.Final;
    }
}


public class StateMachineInstance<TEntity> : StateMachineInstance
{
    public Guid EntityId { get; private set; }
    public TEntity? Entity { get; private set; }

    // EF Core constructor
    protected StateMachineInstance() : base() { }

    public StateMachineInstance(
        StateMachineDefinition definition) : base(definition)
    {
    }

    public StateMachineInstance(
        StateMachineDefinition definition,
        TEntity entity) : base(definition)
    {
        Entity = entity;
    }

}

