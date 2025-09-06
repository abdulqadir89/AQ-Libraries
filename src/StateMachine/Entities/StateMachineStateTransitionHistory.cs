using AQ.Abstractions;
using AQ.Entities;

namespace AQ.StateMachineEntities;

/// <summary>
/// Entity that tracks individual state transitions within a state machine.
/// </summary>
public abstract class StateMachineStateTransitionHistory : Entity
{
    public Guid StateMachineInstanceId { get; private set; }
    public StateMachineInstance StateMachineInstance { get; private set; } = default!;

    public Guid FromStateId { get; private set; }
    public StateMachineState FromState { get; private set; } = default!;

    public Guid ToStateId { get; private set; }
    public StateMachineState ToState { get; private set; } = default!;

    public Guid? TriggerId { get; private set; }
    public StateMachineTrigger? Trigger { get; private set; }
    public bool IsForced { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset TransitionedAt { get; private set; }

    // EF Core constructor
    protected StateMachineStateTransitionHistory() { }

    protected StateMachineStateTransitionHistory(
        Guid stateMachineInstanceId,
        StateMachineState fromState,
        StateMachineState toState,
        StateMachineTrigger? trigger,
        bool isForced,
        string? reason)
    {
        StateMachineInstanceId = stateMachineInstanceId;
        FromStateId = fromState?.Id ?? throw new ArgumentNullException(nameof(fromState));
        FromState = fromState;
        ToStateId = toState?.Id ?? throw new ArgumentNullException(nameof(toState));
        ToState = toState;
        TriggerId = trigger?.Id;
        Trigger = trigger;
        IsForced = isForced;
        Reason = reason;
        TransitionedAt = DateTimeOffset.UtcNow;
    }



    /// <summary>
    /// Gets a formatted description of the transition.
    /// </summary>
    public string GetDescription()
    {
        // No-op entry (no state change and no trigger)
        if (FromStateId == ToStateId && Trigger == null)
            return $"No transition at '{FromState.Name}'{(string.IsNullOrEmpty(Reason) ? "" : $": {Reason}")}";

        return IsForced
            ? $"Forced transition from '{FromState.Name}' to '{ToState.Name}'{(string.IsNullOrEmpty(Reason) ? "" : $": {Reason}")}"
            : $"Transition from '{FromState.Name}' to '{ToState.Name}' via trigger '{Trigger?.Name}'";
    }

    /// <summary>
    /// Checks if this transition matches the specified criteria.
    /// </summary>
    public bool Matches(StateMachineState? fromState = null, StateMachineState? toState = null, StateMachineTrigger? trigger = null)
    {
        return (fromState == null || FromStateId == fromState.Id) &&
               (toState == null || ToStateId == toState.Id) &&
               (trigger == null || TriggerId == trigger.Id);
    }

    /// <summary>
    /// Checks if this transition matches the specified criteria by name.
    /// </summary>
    public bool MatchesByName(string? fromStateName = null, string? toStateName = null, string? triggerName = null)
    {
        return (fromStateName == null || FromState.Name == fromStateName) &&
               (toStateName == null || ToState.Name == toStateName) &&
               (triggerName == null || Trigger?.Name == triggerName);
    }
}


public class StateMachineStateTransitionHistory<TUser, TUserId> : StateMachineStateTransitionHistory where TUser : class, IUser<TUserId> where TUserId : IEquatable<TUserId>
{
    public TUserId UserId { get; protected set; } = default!;
    public TUser User { get; protected set; } = default!;

    public StateMachineStateTransitionHistory() : base() { }

    protected StateMachineStateTransitionHistory(
        Guid stateMachineId,
        StateMachineState fromState,
        StateMachineState toState,
        StateMachineTrigger? trigger,
        bool isForced,
        string? reason,
        TUser user) : base(
            stateMachineId,
            fromState,
            toState,
            trigger,
            isForced,
            reason)
    {
        UserId = user.Id;
        User = user;
    }

    /// <summary>
    /// Creates a new transition history entry for a normal transition.
    /// </summary>
    public static StateMachineStateTransitionHistory Create(
        Guid stateMachineId,
        StateMachineState fromState,
        StateMachineState toState,
        StateMachineTrigger trigger,
        TUser user)
    {
        return new StateMachineStateTransitionHistory<TUser, TUserId>(
            stateMachineId,
            fromState,
            toState,
            trigger,
            isForced: false,
            reason: null,
            user: user);

    }

    /// <summary>
    /// Creates a history entry that records there was no state change (e.g. user commented).
    /// Uses the current state as both From and To so DB non-null constraints remain satisfied.
    /// </summary>
    public static StateMachineStateTransitionHistory CreateNoTransition(
        Guid stateMachineId,
        StateMachineState currentState,
        TUser user,
        string? reason = null)
    {
        return new StateMachineStateTransitionHistory<TUser, TUserId>(
            stateMachineId,
            currentState,
            currentState,
            trigger: null,
            isForced: false,
            reason: reason,
            user: user);
    }

    /// <summary>
    /// Creates a new transition history entry for a forced transition.
    /// </summary>
    public static StateMachineStateTransitionHistory CreateForced(
        Guid stateMachineId,
        StateMachineState fromState,
        StateMachineState toState,
        string reason,
        TUser user)
    {
        return new StateMachineStateTransitionHistory<TUser, TUserId>(
            stateMachineId,
            fromState,
            toState,
            trigger: null,
            isForced: true,
            reason: reason,
            user: user);
    }
}
