using AQ.Events.Domain;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Marker interface for domain events that can be published by state machine transitions.
/// Only events implementing this interface can be configured in a transition's PublishEventTypes.
/// </summary>
public interface IStateMachinePublishEvent : IDomainEvent
{
    /// <summary>
    /// The stable string key used to match this event type when publishing from transitions.
    /// Must be unique across all implementing events in the application.
    /// </summary>
    static abstract string EventTypeKey { get; }
}
