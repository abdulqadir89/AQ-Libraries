using AQ.Events.Domain;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Raised after a state machine instance successfully completes a transition.
/// AggregateId is the state machine instance (e.g. Activity) ID.
/// </summary>
public sealed record StateMachineTransitionedEvent(
    Guid AggregateId,
    Guid DefinitionId,
    int DefinitionVersion,
    Guid ToStateId) : DomainEvent(AggregateId);
