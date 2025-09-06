using AQ.Domain.Entities;

namespace AQ.Domain.StateMachine;

/// <summary>
/// State machine instance that maintains current state and handles transitions.
/// Uses a state machine definition as a template and tracks transition history.
/// </summary>
public abstract class StateMachineInstance : Entity
{
    protected readonly List<StateMachineStateTransitionHistory> _transitionHistory = [];

    public Guid DefinitionId { get; protected set; }
    public StateMachineDefinition Definition { get; protected set; } = default!;
    public StateMachineState CurrentState { get; protected set; } = default!;
    public Guid CurrentStateId { get; protected set; }
    public DateTimeOffset? LastTransitionAt { get; protected set; }

    /// <summary>
    /// Read-only collection of transition history ordered by timestamp.
    /// </summary>
    public IReadOnlyList<StateMachineStateTransitionHistory> TransitionHistory =>
        _transitionHistory.OrderBy(h => h.TransitionedAt).ToList().AsReadOnly();

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
    /// Method for executing a transition. Used by IStateMachineTransitionService.
    /// This method bypasses requirement validation and should not be called directly.
    /// </summary>
    public void ExecuteTransition<TUser, TUserId>(
        StateMachineTransition transition,
        TUser triggeredBy,
        bool isForced = false,
        string? reason = null)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        var previousState = CurrentState;
        CurrentStateId = transition.ToStateId;
        CurrentState = transition.ToState;
        LastTransitionAt = DateTimeOffset.UtcNow;

        // Record transition history
        StateMachineStateTransitionHistory historyEntry;

        if (isForced)
        {
            historyEntry = StateMachineStateTransitionHistory<TUser, TUserId>.CreateForced(
                Id,
                previousState,
                CurrentState,
                reason ?? "Forced transition",
                triggeredBy);
        }
        else
        {
            historyEntry = StateMachineStateTransitionHistory<TUser, TUserId>.Create(
                Id,
                previousState,
                CurrentState,
                transition.Trigger,
                triggeredBy);
        }

        _transitionHistory.Add(historyEntry);

        // Raise domain events
        
    }

    /// <summary>
    /// Method for executing a forced transition to a specific state entity.
    /// Used by IStateMachineTransitionService.
    /// </summary>
    public void ExecuteForcedTransition<TUser, TUserId>(
        StateMachineState targetState,
        string reason,
        TUser triggeredBy)
        where TUser : class, IUser<TUserId>
        where TUserId : IEquatable<TUserId>
    {
        if (targetState == null)
            throw new ArgumentNullException(nameof(targetState));

        var previousState = CurrentState;
        CurrentStateId = targetState.Id;
        CurrentState = targetState;
        LastTransitionAt = DateTimeOffset.UtcNow;

        // Record transition history with forced flag
        var historyEntry = StateMachineStateTransitionHistory<TUser, TUserId>.CreateForced(
            Id,
            previousState,
            CurrentState,
            reason,
            triggeredBy);

        _transitionHistory.Add(historyEntry);

        // Raise domain event for forced transition
        
    }

    /// <summary>
    /// Gets all available trigger entities from the current state.
    /// </summary>
    public IEnumerable<StateMachineTrigger> GetAvailableTriggers()
    {
        return Definition.Transitions.Where(t => t.FromStateId == CurrentStateId)
                          .Select(t => t.Trigger)
                          .Distinct();
    }

    /// <summary>
    /// Gets all available transitions from the current state.
    /// </summary>
    public IEnumerable<StateMachineTransition> GetAvailableTransitions()
    {
        return Definition.Transitions.Where(t => t.FromStateId == CurrentStateId);
    }

    /// <summary>
    /// Gets all requirements for a specific trigger from the current state.
    /// </summary>
    public IEnumerable<IStateMachineTransitionRequirement> GetRequirementsForTrigger(StateMachineTrigger trigger)
    {
        if (trigger == null)
            return [];

        return Definition.Transitions
            .Where(t => t.FromStateId == CurrentStateId && t.TriggerId == trigger.Id)
            .SelectMany(t => t.Requirements ?? [])
            .Distinct();
    }

    /// <summary>
    /// Gets all requirements for all available transitions from the current state.
    /// </summary>
    public IEnumerable<IStateMachineTransitionRequirement> GetAllRequirementsFromCurrentState()
    {
        return Definition.Transitions
            .Where(t => t.FromStateId == CurrentStateId)
            .SelectMany(t => t.Requirements ?? [])
            .Distinct();
    }

    /// <summary>
    /// Gets transitions that can be triggered with the specified trigger from the current state.
    /// </summary>
    public IEnumerable<StateMachineTransition> GetTransitionsForTrigger(StateMachineTrigger trigger)
    {
        if (trigger == null)
            return [];

        return Definition.Transitions.Where(t =>
            t.FromStateId == CurrentStateId && t.TriggerId == trigger.Id);
    }

    /// <summary>
    /// Checks if a specific trigger entity is available from the current state.
    /// </summary>
    public bool CanTrigger(StateMachineTrigger trigger)
    {
        if (trigger == null)
            return false;

        return Definition.Transitions.Any(t =>
            t.FromStateId == CurrentStateId && t.TriggerId == trigger.Id);
    }

    /// <summary>
    /// Gets the current state object.
    /// </summary>
    public StateMachineState? GetCurrentState()
    {
        return CurrentState;
    }

    /// <summary>
    /// Gets a state by name.
    /// </summary>
    public StateMachineState? GetState(string stateName)
    {
        return Definition.States.FirstOrDefault(s => s.Name == stateName);
    }

    /// <summary>
    /// Gets a trigger by name.
    /// </summary>
    public StateMachineTrigger? GetTrigger(string triggerName)
    {
        return Definition.Triggers.FirstOrDefault(t => t.Name == triggerName);
    }

    /// <summary>
    /// Checks if the state machine is in a final state.
    /// </summary>
    public bool IsInFinalState()
    {
        var currentState = GetCurrentState();
        return currentState?.Category == StateMachineStateCategory.Final;
    }

    /// <summary>
    /// Checks if a transition can be made to a specific state using a specific trigger.
    /// </summary>
    public bool CanTransitionTo(StateMachineState targetState, StateMachineTrigger trigger)
    {
        if (targetState == null || trigger == null)
            return false;

        return Definition.Transitions.Any(t =>
            t.FromStateId == CurrentStateId &&
            t.ToStateId == targetState.Id &&
            t.TriggerId == trigger.Id);
    }

    /// <summary>
    /// Gets the transition entity for a specific trigger and target state from the current state.
    /// </summary>
    public StateMachineTransition? GetTransition(StateMachineTrigger trigger, StateMachineState targetState)
    {
        if (trigger == null || targetState == null)
            return null;

        return Definition.Transitions.FirstOrDefault(t =>
            t.FromStateId == CurrentStateId &&
            t.TriggerId == trigger.Id &&
            t.ToStateId == targetState.Id);
    }
}


public class StateMachineInstance<TEntity> : StateMachineInstance
{
    public Guid EntityId { get; private set; }
    public TEntity Entity { get; private set; } = default!;

    // EF Core constructor
    protected StateMachineInstance() : base() { }

    public StateMachineInstance(
        StateMachineDefinition definition,
        TEntity entity) : base(definition)
    {
        Entity = entity;
    }

}
