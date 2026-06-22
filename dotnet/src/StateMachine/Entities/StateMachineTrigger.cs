using AQ.Abstractions;
using AQ.Entities;
using AQ.Utilities.Search;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Defines the types of triggers that can be used in a state machine.
/// </summary>
public enum StateMachineTriggerType
{
    /// <summary>
    /// Manual trigger that requires explicit user or system action.
    /// </summary>
    Manual,

    /// <summary>
    /// Time-based trigger that fires after a specified duration.
    /// </summary>
    Timer,

    /// <summary>
    /// Event-based trigger that fires when a specific event occurs.
    /// </summary>
    Event,

    /// <summary>
    /// Signal-based trigger that fires when a specific signal is received.
    /// </summary>
    Signal,

    /// <summary>
    /// Condition-based trigger that fires when a condition becomes true.
    /// </summary>
    Condition
}

/// <summary>
/// Represents a trigger within a state machine definition that causes transitions between states.
/// </summary>
public class StateMachineTrigger : Entity
{
    public Guid StateMachineDefinitionId { get; private set; }
    public StateMachineDefinition? StateMachineDefinition { get; private set; }
    [Searchable]
    public string Name { get; private set; } = default!;
    [Searchable]
    public string? Description { get; private set; }
    public StateMachineTriggerType Type { get; private set; }
    public bool IsRecordsOnly { get; private set; }
    public string? EventType { get; private set; }
    public string? TriggerMetadataJson { get; private set; }

    // EF Core constructor
    protected StateMachineTrigger() { }

    protected StateMachineTrigger(
        StateMachineDefinition definition,
        string name,
        string? description = null,
        StateMachineTriggerType type = StateMachineTriggerType.Manual,
        bool isRecordsOnly = false,
        string? eventType = null,
        string? triggerMetadataJson = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        StateMachineDefinitionId = definition.Id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Type = type;
        IsRecordsOnly = isRecordsOnly;
        EventType = eventType;
        TriggerMetadataJson = triggerMetadataJson;
    }

    /// <summary>
    /// Creates a new trigger for a state machine definition.
    /// </summary>
    public static StateMachineTrigger Create(
        StateMachineDefinition definition,
        string name,
        string? description = null,
        StateMachineTriggerType type = StateMachineTriggerType.Manual,
        bool isRecordsOnly = false,
        string? eventType = null,
        string? triggerMetadataJson = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trigger name cannot be null or empty.", nameof(name));

        return new StateMachineTrigger(definition, name.Trim(), description?.Trim(), type, isRecordsOnly, eventType, triggerMetadataJson);
    }

    /// <summary>
    /// Updates the trigger's name, description, type, and metadata.
    /// </summary>
    public void Update(
        string? name = null,
        string? description = null,
        StateMachineTriggerType? type = null,
        bool? isRecordsOnly = null,
        string? eventType = null,
        string? triggerMetadataJson = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        Description = description?.Trim();

        if (type.HasValue)
            Type = type.Value;

        if (isRecordsOnly.HasValue)
            IsRecordsOnly = isRecordsOnly.Value;

        if (eventType != null)
            EventType = eventType;

        if (triggerMetadataJson != null)
            TriggerMetadataJson = triggerMetadataJson;
    }

    public override string ToString() => $"{Name} ({Type})";
}

public class AuditableStateMachineTrigger<TUser> : StateMachineTrigger, IAuditable<TUser>
    where TUser : class
{
    public Guid? CreatedById { get; private set; }
    public Guid? UpdatedById { get; private set; }
    public TUser? CreatedBy { get; private set; }
    public TUser? UpdatedBy { get; private set; }

    private AuditableStateMachineTrigger() : base() { }

    private AuditableStateMachineTrigger(
        StateMachineDefinition definition,
        string name,
        string? description,
        StateMachineTriggerType type,
        bool isRecordsOnly,
        string? eventType,
        string? triggerMetadataJson)
        : base(definition, name, description, type, isRecordsOnly, eventType, triggerMetadataJson) { }

    public static new AuditableStateMachineTrigger<TUser> Create(
        StateMachineDefinition definition,
        string name,
        string? description = null,
        StateMachineTriggerType type = StateMachineTriggerType.Manual,
        bool isRecordsOnly = false,
        string? eventType = null,
        string? triggerMetadataJson = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trigger name cannot be null or empty.", nameof(name));

        return new AuditableStateMachineTrigger<TUser>(definition, name.Trim(), description?.Trim(), type, isRecordsOnly, eventType, triggerMetadataJson);
    }

    public void SetCreatedBy(Guid? userId) => CreatedById ??= userId;

    public void SetUpdatedBy(Guid? userId)
    {
        UpdatedById = userId;
        SetUpdatedTimestamp();
    }
}

