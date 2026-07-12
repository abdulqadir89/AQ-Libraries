using AQ.Abstractions;
using AQ.Entities;
using AQ.Utilities.Search;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a state within a state machine definition.
/// </summary>
public class StateMachineState : Entity
{
    public Guid StateMachineDefinitionId { get; private set; }
    public StateMachineDefinition? StateMachineDefinition { get; private set; }
    [Searchable]
    public string Name { get; private set; } = default!;
    [Searchable]
    public string? Description { get; private set; }

    /// <summary>
    /// True if this is a terminal state — no outgoing transitions, instance considered complete.
    /// "Initial" is no longer a state-level concept: which state(s) an instance can start at is
    /// determined entirely by entry transitions (FromState=null), which support multiple entry
    /// points per definition.
    /// </summary>
    public bool IsFinal { get; private set; }

    // EF Core constructor
    protected StateMachineState() { }

    protected StateMachineState(
        StateMachineDefinition definition,
        string name,
        string? description = null,
        bool isFinal = false)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        StateMachineDefinitionId = definition.Id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        IsFinal = isFinal;
    }

    /// <summary>
    /// Creates a new state for a state machine definition.
    /// </summary>
    public static StateMachineState Create(
        StateMachineDefinition definition,
        string name,
        string? description = null,
        bool isFinal = false)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("State name cannot be null or empty.", nameof(name));

        return new StateMachineState(definition, name.Trim(), description?.Trim(), isFinal);
    }

    /// <summary>
    /// Updates the state's name, description and final flag.
    /// </summary>
    public void Update(string? name = null, string? description = null, bool? isFinal = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        Description = description?.Trim();

        if (isFinal.HasValue)
            IsFinal = isFinal.Value;
    }

    public override string ToString() => Name;
}

public class AuditableStateMachineState<TUser> : StateMachineState, IAuditable<TUser>
    where TUser : class
{
    public Guid? CreatedById { get; private set; }
    public Guid? UpdatedById { get; private set; }
    public TUser? CreatedBy { get; private set; }
    public TUser? UpdatedBy { get; private set; }

    private AuditableStateMachineState() : base() { }

    private AuditableStateMachineState(
        StateMachineDefinition definition,
        string name,
        string? description,
        bool isFinal)
        : base(definition, name, description, isFinal) { }

    public static new AuditableStateMachineState<TUser> Create(
        StateMachineDefinition definition,
        string name,
        string? description = null,
        bool isFinal = false)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("State name cannot be null or empty.", nameof(name));

        return new AuditableStateMachineState<TUser>(definition, name.Trim(), description?.Trim(), isFinal);
    }

    public void SetCreatedBy(Guid? userId) => CreatedById ??= userId;

    public void SetUpdatedBy(Guid? userId)
    {
        UpdatedById = userId;
        SetUpdatedTimestamp();
    }
}

