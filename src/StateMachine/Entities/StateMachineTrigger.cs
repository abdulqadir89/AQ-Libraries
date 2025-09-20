using AQ.Entities;

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
    public StateMachineDefinition StateMachineDefinition { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public StateMachineTriggerType Type { get; private set; }

    // EF Core constructor
    private StateMachineTrigger() { }

    private StateMachineTrigger(
        Guid stateMachineDefinitionId,
        string name,
        string? description = null,
        StateMachineTriggerType type = StateMachineTriggerType.Manual)
    {
        StateMachineDefinitionId = stateMachineDefinitionId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Type = type;
    }

    /// <summary>
    /// Creates a new trigger for a state machine definition.
    /// </summary>
    public static StateMachineTrigger Create(
        Guid stateMachineDefinitionId,
        string name,
        string? description = null,
        StateMachineTriggerType type = StateMachineTriggerType.Manual)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trigger name cannot be null or empty.", nameof(name));

        return new StateMachineTrigger(stateMachineDefinitionId, name.Trim(), description?.Trim(), type);
    }

    /// <summary>
    /// Updates the trigger's description and type.
    /// </summary>
    public void Update(
        string? description = null,
        StateMachineTriggerType? type = null)
    {
        Description = description?.Trim();

        if (type.HasValue)
            Type = type.Value;
    }

    public override string ToString() => $"{Name} ({Type})";
}

