using System.Text.Json;
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
    public DateTimeOffset? CurrentStateEnteredAt { get; protected set; }

    public ICollection<StateMachineStateTransitionHistory> TransitionHistory { get; protected set; } = [];

    /// <summary>
    /// JSON-serialized list of <see cref="StateMachineMigrationRecord"/>, appended to by
    /// <see cref="MigrateToDefinition"/>. Definition migrations do not produce a
    /// <see cref="StateMachineStateTransitionHistory"/> row, so this is the only record of when
    /// and between which states/definitions a migration occurred.
    /// </summary>
    public string? MigrationHistory { get; protected set; }

    private static readonly JsonSerializerOptions MigrationHistorySerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // EF Core constructor
    protected StateMachineInstance() { }

    protected StateMachineInstance(
        StateMachineDefinition definition)
    {
        DefinitionId = definition?.Id ?? throw new ArgumentNullException(nameof(definition));

        // CurrentStateId is intentionally left unset here — an instance has no state until a
        // transition (including the initial entry transition, FromState=null) is executed
        // against it. Construction alone must not imply a state.
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
        CurrentStateEnteredAt = DateTimeOffset.UtcNow;

        // Raise domain event for revert operation if needed
    }

    /// <summary>
    /// Gets all available trigger entities from the current state.
    /// Includes both state-changing transitions and non-state-changing (trigger-only) transitions.
    /// </summary>
    public IEnumerable<StateMachineTrigger> GetAvailableTriggers()
    {
        // Final states have no outgoing transitions
        if (CurrentState?.IsFinal == true)
            return [];

        // Get transitions from current state, plus global IsRecordsOnly triggers
        var stateSpecificTransitions = Definition!.Transitions
            .Where(t => t.FromStateId == CurrentStateId)
            .Select(t => Definition.Triggers.FirstOrDefault(tr => tr.Id == t.TriggerId))
            .OfType<StateMachineTrigger>();

        var recordsOnlyTriggers = Definition.GetGlobalTriggers();

        return stateSpecificTransitions.Concat(recordsOnlyTriggers).Distinct();
    }

    /// <summary>
    /// Gets all available transitions from the current state.
    /// </summary>
    public IEnumerable<StateMachineTransition> GetAvailableTransitions()
    {
        // Final states have no outgoing transitions
        if (CurrentState?.IsFinal == true)
            return [];

        // Only return transitions from current state (not IsRecordsOnly triggers — they have no transition)
        return Definition!.Transitions.Where(t => t.FromStateId == CurrentStateId);
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
            .Where(t => t.FromStateId == CurrentStateId && t.TriggerId == trigger.Id)
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
            .Where(t => t.FromStateId == CurrentStateId)
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

        return Definition!.Transitions.Where(t => t.FromStateId == CurrentStateId && t.TriggerId == trigger.Id);
    }

    /// <summary>
    /// Resolves a trigger to find the matching transition from the current state.
    /// If trigger is IsRecordsOnly, returns null for transition and true for isRecordsOnly.
    /// If trigger is not IsRecordsOnly and no transition is found, throws an exception.
    /// </summary>
    public (StateMachineTransition? transition, bool isRecordsOnly) ResolveTransition(StateMachineTrigger trigger)
    {
        if (trigger == null)
            throw new ArgumentNullException(nameof(trigger));

        if (trigger.IsRecordsOnly)
            return (null, true);

        var match = GetTransitionsForTrigger(trigger).FirstOrDefault();

        if (match is null)
            throw new InvalidOperationException(
                $"Trigger '{trigger.Name}' has no transition defined from state '{CurrentState?.Name}'. " +
                "Ensure the state machine definition is correctly configured.");

        return (match, false);
    }

    /// <summary>
    /// Checks if the state machine is in a final state.
    /// </summary>
    public bool IsInFinalState()
    {
        return CurrentState!.IsFinal;
    }

    /// <summary>
    /// Raises a domain event signalling that a transition completed.
    /// Call this from the transition service after a successful transition, before saving.
    /// </summary>
    public void RaiseTransitionedEvent(Guid definitionId, int definitionVersion, Guid toStateId)
        => AddDomainEvent(new StateMachineTransitionedEvent(Id, definitionId, definitionVersion, toStateId));

    /// <summary>
    /// Gets the full history of definition migrations performed on this instance, oldest first.
    /// </summary>
    public IReadOnlyList<StateMachineMigrationRecord> GetMigrationHistory()
    {
        return string.IsNullOrEmpty(MigrationHistory)
            ? []
            : JsonSerializer.Deserialize<List<StateMachineMigrationRecord>>(MigrationHistory, MigrationHistorySerializerOptions) ?? [];
    }

    private void AppendMigrationRecord(StateMachineMigrationRecord record)
    {
        var records = GetMigrationHistory().ToList();
        records.Add(record);
        MigrationHistory = JsonSerializer.Serialize(records, MigrationHistorySerializerOptions);
    }

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

        var previousDefinitionId = DefinitionId;
        var previousStateId = CurrentStateId;
        var migratedAt = DateTimeOffset.UtcNow;

        // A migration is a definition swap, not a real state change — CurrentStateEnteredAt and
        // LastTransitionAt are deliberately NOT reset here, so the instance's true accumulated
        // dwell time in its (conceptual) current state survives the migration. MigratedAt on the
        // migration record marks the boundary instant for analytics purposes instead.
        DefinitionId = newDefinition.Id;
        Definition = newDefinition;
        CurrentStateId = newState.Id;
        CurrentState = newState;

        AppendMigrationRecord(new StateMachineMigrationRecord(
            previousDefinitionId, newDefinition.Id, previousStateId, newState.Id, migratedAt));
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

public abstract class AuditableStateMachineInstance<TUser> : StateMachineInstance, IAuditable<TUser>
    where TUser : class
{
    public Guid? CreatedById { get; private set; }
    public Guid? UpdatedById { get; private set; }
    public TUser? CreatedBy { get; private set; }
    public TUser? UpdatedBy { get; private set; }

    protected AuditableStateMachineInstance() : base() { }

    protected AuditableStateMachineInstance(StateMachineDefinition definition) : base(definition) { }

    public void SetCreatedBy(Guid? userId) => CreatedById ??= userId;

    public void SetUpdatedBy(Guid? userId)
    {
        UpdatedById = userId;
        SetUpdatedTimestamp();
    }
}

