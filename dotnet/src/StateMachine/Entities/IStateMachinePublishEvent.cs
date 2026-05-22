using AQ.Events.Domain;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Marker interface for domain events that can be published by state machine transitions.
/// Only events implementing this interface can be configured in a transition's PublishEventTypes.
/// </summary>
/// <remarks>
/// <para>
/// Implementing events <strong>must have exactly one constructor parameter</strong>:
/// <c>Guid state machine instance id</c>, which is passed as the <c>AggregateId</c> to the base <c>DomainEvent</c>.
/// </para>
/// <para>
/// No additional properties should be added. At raise-time the state machine only knows the
/// state machine instance ID. Handlers are responsible for fetching any other data they need from the database.
/// </para>
/// <example>
/// <code>
/// public sealed record MyEvent(Guid StateMachineInstanceId)
///     : DomainEvent(StateMachineInstanceId), IStateMachinePublishEvent
/// {
///     public static string EventTypeKey => "MyEvent";
/// }
/// </code>
/// </example>
/// </remarks>
public interface IStateMachinePublishEvent : IDomainEvent
{
    /// <summary>
    /// The stable string key used to match this event type when publishing from transitions.
    /// Must be unique across all implementing events in the application.
    /// </summary>
    static abstract string EventTypeKey { get; }
}
