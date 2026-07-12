using AQ.Events.Domain;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Marker interface for domain events that can be used as triggers in state machine definitions.
/// Only events implementing this interface can be linked to a StateMachineTrigger via EventType.
/// </summary>
public interface IStateMachineTriggerEvent : IDomainEvent
{
    /// <summary>
    /// The stable string key used to match this event to a StateMachineTrigger.EventType.
    /// Must be unique across all implementing events in the application.
    /// </summary>
    static abstract string EventTypeKey { get; }

    /// <summary>
    /// Zero or more state machine instance ids already known to be related to this event (e.g. a
    /// GatePass's ActivityId once set, or a Risk assessment cycle's linked Activities).
    /// A generic handler evaluates every published definition with a trigger matching EventType,
    /// independently: each id in this list that belongs to the definition gets transitioned; and
    /// separately, if the definition's matched trigger is an entry trigger (FromState=null), a
    /// new instance is ALSO created via the entry path — these are not mutually exclusive, both
    /// run wherever they apply. A definition whose matched trigger is not an entry trigger and
    /// has no id in this list belonging to it is skipped (nothing to transition, no way to create).
    /// </summary>
    IReadOnlyList<Guid>? StateMachineInstanceIds => null;
}
