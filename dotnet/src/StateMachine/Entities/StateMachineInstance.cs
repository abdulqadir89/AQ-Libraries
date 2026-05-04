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

        // Set current state to the definition's initial state
        var initialState = definition.InitialState ?? throw new InvalidOperationException("Definition must have an initial state.");
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
            .Select(t => Definition.Triggers.FirstOrDefault(tr => tr.Id == t.TriggerId))
            .OfType<StateMachineTrigger>()
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

    /// <summary>
    /// Raises a domain event signalling that a transition completed.
    /// Call this from the transition service after a successful transition, before saving.
    /// </summary>
    public void RaiseTransitionedEvent(Guid definitionId, int definitionVersion, Guid toStateId)
        => AddDomainEvent(new StateMachineTransitionedEvent(Id, definitionId, definitionVersion, toStateId));

    /// <summary>
    /// Migrates this instance to a new state machine definition using the mapping stored in
    /// <see cref="StateMachineDefinition.PreviousVersionStateMapping"/>.
    /// State IDs — not names — are used to resolve the current state in the new definition,
    /// since names are mutable but IDs are stable.
    /// </summary>
    /// <param name="newDefinition">
    /// The target definition to migrate to. Must have a <see cref="StateMachineDefinition.PreviousVersionStateMapping"/>
    /// entry for the current <see cref="CurrentStateId"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newDefinition"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current state ID has no entry in the definition's mapping, or the mapped
    /// target state does not exist in the new definition.
    /// </exception>
    public void MigrateToDefinition(StateMachineDefinition newDefinition)
    {
        if (newDefinition is null) throw new ArgumentNullException(nameof(newDefinition));

        var mapping = newDefinition.PreviousVersionStateMapping;

        if (!mapping.TryGetValue(CurrentStateId, out var newStateId))
            throw new InvalidOperationException(
                $"No mapping found for current state ID '{CurrentStateId}' in the target definition's PreviousVersionStateMapping.");

        var newState = newDefinition.States.FirstOrDefault(s => s.Id == newStateId)
            ?? throw new InvalidOperationException(
                $"Mapped target state ID '{newStateId}' does not exist in the target definition.");

        DefinitionId = newDefinition.Id;
        Definition = newDefinition;
        CurrentStateId = newState.Id;
        CurrentState = newState;
        LastTransitionAt = DateTimeOffset.UtcNow;
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
        // Special case: Entity nav prop is set here because EntityId has no corresponding
        // FK assignment path for the generic TEntity (no IEntity constraint).
        // EF Core will override this on load.
        Entity = entity;
    }

}

