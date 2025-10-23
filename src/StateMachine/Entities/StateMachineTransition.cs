using System.Text.Json;
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
    public StateMachineTrigger? Trigger { get; private set; }
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
            ? $"{FromState!.Name} -> {ToState!.Name} ({Trigger!.Name})"
            : $"Trigger: {Trigger!.Name}";

    #region JSON Serialization for Persistence

    /// <summary>
    /// Serializes requirements to JSON for database storage.
    /// </summary>
    public static string? SerializeRequirements(IEnumerable<IStateMachineTransitionRequirement>? requirements)
    {
        if (requirements == null) return null;

        var requirementDict = new Dictionary<string, object>();

        foreach (var requirement in requirements)
        {
            var typeName = requirement.GetType().Name;
            var data = JsonSerializer.SerializeToElement(requirement);
            requirementDict[typeName] = data;
        }

        return JsonSerializer.Serialize(requirementDict);
    }

    /// <summary>
    /// Deserializes requirements from JSON database storage.
    /// </summary>
    public static IEnumerable<IStateMachineTransitionRequirement>? DeserializeRequirements(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var requirementDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (requirementDict == null) return null;

            var requirements = new List<IStateMachineTransitionRequirement>();

            foreach (var kvp in requirementDict)
            {
                var requirementType = kvp.Key;
                var data = kvp.Value;

                // Create a generic requirement wrapper for JSON storage
                var requirement = new JsonStoredRequirement
                {
                    RequirementTypeName = requirementType,
                    Data = JsonSerializer.Deserialize<Dictionary<string, object>>(data.ToString() ?? "{}") ?? new Dictionary<string, object>()
                };

                requirements.Add(requirement);
            }

            return requirements;
        }
        catch
        {
            // Return empty collection if deserialization fails
            return [];
        }
    }

    /// <summary>
    /// Serializes effects to JSON for database storage.
    /// </summary>
    public static string? SerializeEffects(IEnumerable<IStateMachineTransitionEffect>? effects)
    {
        if (effects == null) return null;

        var effectDict = new Dictionary<string, object>();

        foreach (var effect in effects)
        {
            var typeName = effect.GetType().Name;
            var data = JsonSerializer.SerializeToElement(effect);
            effectDict[typeName] = data;
        }

        return JsonSerializer.Serialize(effectDict);
    }

    /// <summary>
    /// Deserializes effects from JSON database storage.
    /// </summary>
    public static IEnumerable<IStateMachineTransitionEffect>? DeserializeEffects(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var effectDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (effectDict == null) return null;

            var effects = new List<IStateMachineTransitionEffect>();

            foreach (var kvp in effectDict)
            {
                var effectType = kvp.Key;
                var data = kvp.Value;

                // Create a generic effect wrapper for JSON storage
                var effect = new JsonStoredEffect
                {
                    EffectTypeName = effectType,
                    Data = JsonSerializer.Deserialize<Dictionary<string, object>>(data.ToString() ?? "{}") ?? new Dictionary<string, object>()
                };

                effects.Add(effect);
            }

            return effects;
        }
        catch
        {
            // Return empty collection if deserialization fails
            return [];
        }
    }

    #endregion

    #region Internal Storage Classes

    /// <summary>
    /// Internal class for storing requirements as JSON in the database.
    /// This allows storage of any requirement type without requiring specific configurations.
    /// The requirement type is determined by the actual implementation class type.
    /// </summary>
    internal class JsonStoredRequirement : IStateMachineTransitionRequirement
    {
        public string RequirementTypeName { get; set; } = default!;
        public Dictionary<string, object> Data { get; set; } = [];

        /// <summary>
        /// Gets the requirement type name based on the stored type name.
        /// </summary>
        public string GetRequirementTypeName() => RequirementTypeName;
    }

    /// <summary>
    /// Internal class for storing effects as JSON in the database.
    /// This allows storage of any effect type without requiring specific configurations.
    /// The effect type is determined by the actual implementation class type.
    /// </summary>
    internal class JsonStoredEffect : IStateMachineTransitionEffect
    {
        public string EffectTypeName { get; set; } = default!;
        public Dictionary<string, object> Data { get; set; } = [];

        /// <summary>
        /// Gets the effect type name based on the stored type name.
        /// </summary>
        public string GetEffectTypeName() => EffectTypeName;
    }

    #endregion
}

