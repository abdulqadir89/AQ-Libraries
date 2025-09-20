using AQ.Abstractions;
using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a state within a state machine definition.
/// </summary>
public class StateMachineState : Entity, IHasCategory<StateMachineStateCategory>
{
    public Guid StateMachineDefinitionId { get; private set; }
    public StateMachineDefinition StateMachineDefinition { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public StateMachineStateCategory Category { get; private set; } = StateMachineStateCategory.Intermediate;

    // EF Core constructor
    private StateMachineState() { }

    private StateMachineState(
        Guid stateMachineDefinitionId,
        string name,
        string? description = null,
        StateMachineStateCategory category = StateMachineStateCategory.Intermediate)
    {
        StateMachineDefinitionId = stateMachineDefinitionId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Category = category;
    }

    /// <summary>
    /// Creates a new state for a state machine definition.
    /// </summary>
    public static StateMachineState Create(
        Guid stateMachineDefinitionId,
        string name,
        string? description = null,
        StateMachineStateCategory category = StateMachineStateCategory.Intermediate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("State name cannot be null or empty.", nameof(name));

        return new StateMachineState(stateMachineDefinitionId, name.Trim(), description?.Trim(), category);
    }

    /// <summary>
    /// Updates the state's name, description and category.
    /// </summary>
    public void Update(string? name = null, string? description = null, StateMachineStateCategory? category = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        Description = description?.Trim();

        if (category.HasValue)
            Category = category.Value;
    }

    /// <summary>
    /// Sets the category of the state.
    /// </summary>
    public void SetCategory(StateMachineStateCategory category) => Category = category;

    public override string ToString() => Name;
}

