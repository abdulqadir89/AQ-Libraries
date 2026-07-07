namespace AQ.StateMachine.Entities;

/// <summary>
/// Records one definition migration performed on a <see cref="StateMachineInstance"/> via
/// <see cref="StateMachineInstance.MigrateToDefinition"/>. Migrations mutate the instance's
/// current state/definition in place without writing a <see cref="StateMachineStateTransitionHistory"/>
/// row, so this is the only record of the state/definition change and its timestamp.
/// </summary>
public sealed record StateMachineMigrationRecord(
    Guid FromDefinitionId,
    Guid ToDefinitionId,
    Guid FromStateId,
    Guid ToStateId,
    DateTimeOffset MigratedAt);
