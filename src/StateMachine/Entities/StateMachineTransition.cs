using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a transition between states in a state machine definition.
/// </summary>
public class StateMachineTransition : Entity
{
    public Guid StateMachineDefinitionId { get; private set; }
    public StateMachineState? FromState { get; private set; }
    public Guid? FromStateId { get; private set; }
    public StateMachineState? ToState { get; private set; }
    public Guid? ToStateId { get; private set; }
    public StateMachineTrigger Trigger { get; private set; } = default!;
    public Guid TriggerId { get; private set; }
    public string? Description { get; private set; }

    public IEnumerable<IStateMachineTransitionRequirement>? Requirements { get; private set; }
    public IEnumerable<IStateMachineTransitionEffect>? Effects { get; private set; }

    // EF Core constructor
    private StateMachineTransition() { }

    private StateMachineTransition(
        StateMachineDefinition definition,
        StateMachineState? fromState,
        StateMachineState? toState,
        StateMachineTrigger trigger,
        string? description = null,
        IEnumerable<IStateMachineTransitionRequirement>? requirements = null,
        IEnumerable<IStateMachineTransitionEffect>? effects = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        StateMachineDefinitionId = definition.Id;
        FromState = fromState;
        FromStateId = fromState?.Id;
        ToState = toState;
        ToStateId = toState?.Id;
        Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        TriggerId = trigger.Id;
        Description = description?.Trim();
        Requirements = requirements;
        Effects = effects;
    }

    /// <summary>
    /// Creates a new transition for a state machine definition.
    /// </summary>
    public static StateMachineTransition Create(
        StateMachineDefinition definition,
        StateMachineState? fromState,
        StateMachineState? toState,
        StateMachineTrigger trigger,
        string? description = null,
        IEnumerable<IStateMachineTransitionRequirement>? requirements = null,
        IEnumerable<IStateMachineTransitionEffect>? effects = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (trigger == null)
            throw new ArgumentNullException(nameof(trigger));

        return new StateMachineTransition(
            definition,
            fromState,
            toState,
            trigger,
            description?.Trim(),
            requirements,
            effects);
    }

    /// <summary>
    /// Updates the transition's properties.
    /// </summary>
    public void Update(
        string? description = null,
        IEnumerable<IStateMachineTransitionRequirement>? requirements = null,
        IEnumerable<IStateMachineTransitionEffect>? effects = null)
    {
        Description = description?.Trim();
        Requirements = requirements;
        Effects = effects;
    }

    /// <summary>
    /// Checks if this transition has any requirements that need to be evaluated.
    /// </summary>
    public bool HasRequirements => Requirements?.Any() == true;

    /// <summary>
    /// Gets the count of requirements for this transition.
    /// </summary>
    public int RequirementCount => Requirements?.Count() ?? 0;

    /// <summary>
    /// Checks if this transition has any effects that need to be executed.
    /// </summary>
    public bool HasEffects => Effects?.Any() == true;

    /// <summary>
    /// Gets the count of effects for this transition.
    /// </summary>
    public int EffectCount => Effects?.Count() ?? 0;

    /// <summary>
    /// Gets the types of data entities that need to be collected from users for this transition.
    /// Returns an empty collection if no data collection is required.
    /// </summary>
    public IEnumerable<Type> GetRequiredDataTypes()
    {
        if (!HasRequirements)
            return Enumerable.Empty<Type>();

        return Requirements!
            .OfType<StateMachineTransitionRequirement>()
            .SelectMany(r => r.GetRequiredDataTypes())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Checks if this transition requires any user-provided data to be collected.
    /// </summary>
    public bool RequiresUserData => GetRequiredDataTypes().Any();

    /// <summary>
    /// Indicates whether this transition changes state (has both from and to states).
    /// </summary>
    public bool ChangesState => FromState != null && ToState != null;

    /// <summary>
    /// Indicates whether this is a trigger-only transition (no state change).
    /// </summary>
    public bool IsTriggerOnly => FromState == null || ToState == null;

    /// <summary>
    /// Adds a requirement to this transition.
    /// </summary>
    public void AddRequirement(IStateMachineTransitionRequirement requirement)
    {
        if (requirement == null)
            throw new ArgumentNullException(nameof(requirement));

        var currentRequirements = Requirements?.ToList() ?? [];
        currentRequirements.Add(requirement);
        Requirements = currentRequirements;
    }

    /// <summary>
    /// Removes a requirement from this transition by type.
    /// </summary>
    public void RemoveRequirement<T>() where T : IStateMachineTransitionRequirement
    {
        var currentRequirements = Requirements?.ToList() ?? [];
        currentRequirements.RemoveAll(r => r.GetType() == typeof(T));
        Requirements = currentRequirements.Any() ? currentRequirements : null;
    }

    /// <summary>
    /// Removes a requirement from this transition by type name.
    /// </summary>
    public void RemoveRequirement(string requirementTypeName)
    {
        if (string.IsNullOrWhiteSpace(requirementTypeName))
            return;

        var currentRequirements = Requirements?.ToList() ?? [];
        currentRequirements.RemoveAll(r => r.GetType().Name == requirementTypeName);
        Requirements = currentRequirements.Any() ? currentRequirements : null;
    }

    /// <summary>
    /// Adds an effect to this transition.
    /// </summary>
    public void AddEffect(IStateMachineTransitionEffect effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        var currentEffects = Effects?.ToList() ?? [];
        currentEffects.Add(effect);
        Effects = currentEffects;
    }

    /// <summary>
    /// Removes an effect from this transition by type.
    /// </summary>
    public void RemoveEffect<T>() where T : IStateMachineTransitionEffect
    {
        var currentEffects = Effects?.ToList() ?? [];
        currentEffects.RemoveAll(e => e.GetType() == typeof(T));
        Effects = currentEffects.Any() ? currentEffects : null;
    }

    /// <summary>
    /// Removes an effect from this transition by type name.
    /// </summary>
    public void RemoveEffect(string effectTypeName)
    {
        if (string.IsNullOrWhiteSpace(effectTypeName))
            return;

        var currentEffects = Effects?.ToList() ?? [];
        currentEffects.RemoveAll(e => e.GetType().Name == effectTypeName);
        Effects = currentEffects.Any() ? currentEffects : null;
    }

    public override string ToString() =>
        ChangesState
            ? $"{FromState!.Name} -> {ToState!.Name} ({Trigger.Name})"
            : $"Trigger: {Trigger.Name}";
}

