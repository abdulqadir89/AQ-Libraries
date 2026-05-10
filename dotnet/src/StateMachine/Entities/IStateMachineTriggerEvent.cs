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
}
