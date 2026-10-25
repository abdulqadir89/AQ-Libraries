using AQ.Entities;
using AQ.Utilities.Search;
using System.Text.Json;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a transition between states in a state machine definition.
/// </summary>
public class StateMachineTransition : Entity
{
    private static readonly JsonSerializerOptions _camelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Guid StateMachineDefinitionId { get; private set; }
    public StateMachineState? FromState { get; private set; }
    public Guid? FromStateId { get; private set; }
    public StateMachineState? ToState { get; private set; }
    public Guid? ToStateId { get; private set; }
    public StateMachineTrigger? Trigger { get; private set; }
    public Guid TriggerId { get; private set; }
    [Searchable]
    public string? Description { get; private set; }

    public IEnumerable<IStateMachineTransitionRequirement>? Requirements { get; private set; }

    public string PublishEventTypesJson { get; private set; } = "[]";

    public string? TransitionMetadataJson { get; private set; }

    /// <summary>
    /// Gets the list of event type keys that this transition will publish when it executes.
    /// </summary>
    public IReadOnlyList<string> PublishEventTypes
    {
        get => JsonSerializer.Deserialize<List<string>>(PublishEventTypesJson) ?? [];
        private set => PublishEventTypesJson = JsonSerializer.Serialize(value);
    }

    // EF Core constructor
    protected StateMachineTransition() { }

    protected StateMachineTransition(
        StateMachineDefinition definition,
        StateMachineState? fromState,
        StateMachineState? toState,
        StateMachineTrigger trigger,
        string? description = null,
        IEnumerable<IStateMachineTransitionRequirement>? requirements = null,
        IEnumerable<string>? publishEventTypes = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        StateMachineDefinitionId = definition.Id;
        FromStateId = fromState?.Id;
        ToStateId = toState?.Id;
        TriggerId = (trigger ?? throw new ArgumentNullException(nameof(trigger))).Id;
        Description = description?.Trim();
        Requirements = Normalize(requirements);
        if (publishEventTypes is not null)
            PublishEventTypes = publishEventTypes.ToList();
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
        IEnumerable<string>? publishEventTypes = null)
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
            publishEventTypes);
    }

    /// <summary>
    /// Updates the transition's properties.
    /// </summary>
    public void Update(
        string? description = null,
        IEnumerable<IStateMachineTransitionRequirement>? requirements = null,
        IEnumerable<string>? publishEventTypes = null)
    {
        Description = description?.Trim();
        Requirements = Normalize(requirements);
        if (publishEventTypes is not null)
            PublishEventTypes = publishEventTypes.ToList();
    }

    /// <summary>
    /// Gets the typed metadata stored on this transition, or null if none is set.
    /// </summary>
    public T? GetMetadata<T>() where T : class
        => TransitionMetadataJson is null
            ? null
            : JsonSerializer.Deserialize<T>(TransitionMetadataJson, _camelCase);

    /// <summary>
    /// Sets typed metadata on this transition. Pass null to clear.
    /// </summary>
    public void SetMetadata<T>(T? value) where T : class
        => TransitionMetadataJson = value is null
            ? null
            : JsonSerializer.Serialize(value, _camelCase);

    /// <summary>
    /// Checks if this transition has any requirements that need to be evaluated.
    /// </summary>
    public bool HasRequirements => Requirements?.Any() == true;

    /// <summary>
    /// Gets the count of requirements for this transition.
    /// </summary>
    public int RequirementCount => Requirements?.Count() ?? 0;

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
    /// Indicates whether this transition changes state (from and to state IDs differ).
    /// Entry transitions have a null FromStateId, so they always count as a state change.
    /// Uses IDs instead of nav props to avoid issues with unloaded entities.
    /// </summary>
    public bool ChangesState => FromStateId != ToStateId;

    /// <summary>
    /// Indicates whether this is a trigger-only transition (no state change).
    /// </summary>
    public bool IsTriggerOnly => !ChangesState;

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

    public override string ToString() =>
        ChangesState
            ? $"{FromState?.Name ?? "(entry)"} -> {ToState!.Name} ({Trigger!.Name})"
            : $"Trigger: {Trigger!.Name}";

    /// <summary>
    /// Serializes requirements to JSON for database storage.
    /// </summary>
    public static string? SerializeRequirements(IEnumerable<IStateMachineTransitionRequirement>? requirements)
        => StateMachineRequirementSerializer.Serialize(requirements);

    /// <summary>
    /// Deserializes requirements from JSON database storage.
    /// </summary>
    public static IEnumerable<IStateMachineTransitionRequirement>? DeserializeRequirements(string? json)
        => StateMachineRequirementSerializer.Deserialize(json);

    // RULE SMC-14
    private static List<IStateMachineTransitionRequirement>? Normalize(IEnumerable<IStateMachineTransitionRequirement>? requirements)
    {
        var list = requirements?.ToList();
        return list is { Count: > 0 } ? list : null;
    }
}

public class AuditableStateMachineTransition<TPrincipal> : StateMachineTransition, IAuditable<TPrincipal>
    where TPrincipal : class
{
    public Guid CreatedById { get; private set; }
    public Guid UpdatedById { get; private set; }
    public TPrincipal? CreatedBy { get; private set; }
    public TPrincipal? UpdatedBy { get; private set; }

    private AuditableStateMachineTransition() : base() { }

    private AuditableStateMachineTransition(
        StateMachineDefinition definition,
        StateMachineState? fromState,
        StateMachineState? toState,
        StateMachineTrigger trigger,
        string? description,
        IEnumerable<IStateMachineTransitionRequirement>? requirements,
        IEnumerable<string>? publishEventTypes)
        : base(definition, fromState, toState, trigger, description, requirements, publishEventTypes) { }

    public static new AuditableStateMachineTransition<TPrincipal> Create(
        StateMachineDefinition definition,
        StateMachineState? fromState,
        StateMachineState? toState,
        StateMachineTrigger trigger,
        string? description = null,
        IEnumerable<IStateMachineTransitionRequirement>? requirements = null,
        IEnumerable<string>? publishEventTypes = null)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));

        return new AuditableStateMachineTransition<TPrincipal>(
            definition, fromState, toState, trigger, description?.Trim(), requirements, publishEventTypes);
    }

    public void SetCreatedBy(Guid principalId)
    {
        if (CreatedById != Guid.Empty)
        {
            return;
        }

        CreatedById = principalId;
        UpdatedById = principalId;
    }

    public void SetUpdatedBy(Guid principalId)
    {
        UpdatedById = principalId;
        SetUpdatedTimestamp();
    }
}

