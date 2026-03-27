using AQ.Abstractions;
using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a reusable state machine definition that contains states, triggers, and transitions.
/// This is the template/blueprint that state machine instances are based on.
/// </summary>
public abstract class StateMachineDefinition : Entity
{
    private readonly List<StateMachineState> _states = [];
    private readonly List<StateMachineTrigger> _triggers = [];
    private readonly List<StateMachineTransition> _transitions = [];

    public int Version { get; private set; }

    /// <summary>
    /// Read-only collection of states in this definition.
    /// </summary>
    public IReadOnlyList<StateMachineState> States => _states.AsReadOnly();

    /// <summary>
    /// Read-only collection of triggers in this definition.
    /// </summary>
    public IReadOnlyList<StateMachineTrigger> Triggers => _triggers.AsReadOnly();

    /// <summary>
    /// Read-only collection of transitions in this definition.
    /// </summary>
    public IReadOnlyList<StateMachineTransition> Transitions => _transitions.AsReadOnly();

    /// <summary>
    /// Gets the initial state of the state machine.
    /// </summary>
    public StateMachineState? InitialState => _states.FirstOrDefault(s => s.Category == StateMachineStateCategory.Initial);

    public StateMachineDefinitionStatus Status { get; private set; }

    /// <summary>
    /// Maps previous-version state IDs to this version's state IDs.
    /// Populated on a new version to enable migrating instances from the previous definition.
    /// Multiple previous-version state IDs may point to the same target state ID.
    /// States in this version that no previous state maps to are valid and require no entry.
    /// </summary>
    public Dictionary<Guid, Guid> PreviousVersionStateMapping { get; private set; } = [];

    // EF Core constructor
    protected StateMachineDefinition() { }

    protected StateMachineDefinition(
        string initialStateName,
        int version = 1)
    {
        Version = version;

        // Create the initial state
        var initialState = StateMachineState.Create(this, initialStateName, category: StateMachineStateCategory.Initial);
        _states.Add(initialState);

        // Add domain event for definition creation
    }



    /// <summary>
    /// Creates a new version of this definition.
    /// </summary>
    public abstract StateMachineDefinition CreateNewVersion(int version);

    /// <summary>
    /// Adds a trigger to this definition. For use by derived classes only.
    /// </summary>
    protected void AddTrigger(StateMachineTrigger trigger)
    {
        _triggers.Add(trigger);
    }

    /// <summary>
    /// Adds a transition to this definition. For use by derived classes only.
    /// </summary>
    protected void AddTransition(StateMachineTransition transition)
    {
        _transitions.Add(transition);
    }

    /// <summary>
    /// Helper method to copy states, triggers, and transitions to a new definition.
    /// </summary>
    protected void CopyDefinitionDataTo(StateMachineDefinition newDefinition)
    {
        if (InitialState == null)
            throw new InvalidOperationException("Cannot create new version without an initial state.");

        // Copy all states except the initial state (which is already created in constructor)
        foreach (var state in _states.Where(s => s.Id != InitialState.Id))
        {
            var newState = StateMachineState.Create(
                newDefinition,
                state.Name,
                state.Description,
                state.Category);
            newDefinition._states.Add(newState);
        }

        // Copy all triggers
        foreach (var trigger in _triggers)
        {
            // Skip triggers already present by name in the new definition (e.g. auto-added in constructor)
            if (newDefinition._triggers.Any(t => t.Name == trigger.Name))
                continue;

            var newTrigger = StateMachineTrigger.Create(
                newDefinition,
                trigger.Name,
                trigger.Description,
                trigger.Type);
            newDefinition._triggers.Add(newTrigger);
        }

        // Copy all transitions with their requirements and descriptions
        foreach (var transition in _transitions)
        {
            // Find corresponding states and triggers by name in the new definition.
            // Nav props may not be loaded; resolve names via ID lookup in the source collections.
            var sourceFromStateName = transition.FromStateId.HasValue
                ? _states.FirstOrDefault(s => s.Id == transition.FromStateId.Value)?.Name
                : null;
            var sourceToStateName = transition.ToStateId.HasValue
                ? _states.FirstOrDefault(s => s.Id == transition.ToStateId.Value)?.Name
                : null;
            var sourceTriggerName = _triggers.FirstOrDefault(t => t.Id == transition.TriggerId)?.Name;

            var newFromState = sourceFromStateName != null
                ? newDefinition._states.FirstOrDefault(s => s.Name == sourceFromStateName)
                : null;
            var newToState = sourceToStateName != null
                ? newDefinition._states.FirstOrDefault(s => s.Name == sourceToStateName)
                : null;
            var newTrigger = sourceTriggerName != null
                ? newDefinition._triggers.FirstOrDefault(t => t.Name == sourceTriggerName)
                : null;

            if (newTrigger != null)
            {
                // Skip transitions whose trigger is already represented (e.g. auto-added global triggers)
                if (newDefinition._transitions.Any(t => t.TriggerId == newTrigger.Id))
                    continue;

                var newTransition = StateMachineTransition.Create(
                    newDefinition,
                    newFromState,
                    newToState,
                    newTrigger,
                    transition.Description,
                    transition.Requirements);
                newDefinition._transitions.Add(newTransition);
            }
        }
    }

    /// <summary>
    /// Gets a state by name.
    /// </summary>
    public StateMachineState? GetState(string stateName)
    {
        return _states.FirstOrDefault(s => s.Name == stateName);
    }

    /// <summary>
    /// Gets a trigger by name.
    /// </summary>
    public StateMachineTrigger? GetTrigger(string triggerName)
    {
        return _triggers.FirstOrDefault(t => t.Name == triggerName);
    }

    /// <summary>
    /// Gets all transitions from a specific state.
    /// </summary>
    public IEnumerable<StateMachineTransition> GetTransitionsFromState(StateMachineState state)
    {
        return state == null ? [] : _transitions.Where(t => t.FromStateId == state.Id);
    }

    /// <summary>
    /// Gets all available triggers from a specific state.
    /// </summary>
    public IEnumerable<StateMachineTrigger> GetAvailableTriggersFromState(StateMachineState state)
    {
        return GetTransitionsFromState(state)
            .Select(t => _triggers.FirstOrDefault(tr => tr.Id == t.TriggerId))
            .OfType<StateMachineTrigger>()
            .Distinct();
    }

    /// <summary>
    /// Generates a Mermaid state diagram representation of the state machine definition.
    /// </summary>
    /// <param name="currentState">Optional current state to highlight in the diagram</param>
    /// <returns>Mermaid diagram as a string</returns>
    public string ToMermaidDiagram(StateMachineState? currentState = null)
    {
        var diagram = new System.Text.StringBuilder();
        diagram.AppendLine("stateDiagram-v2");

        // Add all transitions with their triggers
        foreach (var transition in _transitions)
        {
            // Skip global triggers (null→null) — they don't change state and are listed separately
            if (!transition.FromStateId.HasValue && !transition.ToStateId.HasValue)
                continue;

            var fromStateName = transition.FromStateId.HasValue
                ? SanitizeStateName(_states.FirstOrDefault(s => s.Id == transition.FromStateId.Value)?.Name ?? string.Empty)
                : "[*]";
            var toStateName = transition.ToStateId.HasValue
                ? SanitizeStateName(_states.FirstOrDefault(s => s.Id == transition.ToStateId.Value)?.Name ?? string.Empty)
                : "[*]";
            var triggerName = _triggers.FirstOrDefault(t => t.Id == transition.TriggerId)?.Name ?? string.Empty;

            diagram.AppendLine($"    {fromStateName} --> {toStateName} : {triggerName}");

            // Add note with requirements if they exist
            if (transition.HasRequirements)
            {
                diagram.AppendLine($"    note right of {toStateName}");

                // Add requirements
                var requirements = transition.Requirements!
                    .Select(r => GetRequirementDescription(r))
                    .Where(desc => !string.IsNullOrEmpty(desc));

                foreach (var requirement in requirements)
                {
                    diagram.AppendLine($"      Require: {requirement}");
                }

                diagram.AppendLine("    end note");
                diagram.AppendLine();
            }
        }

        // Highlight current state if provided
        if (currentState != null)
        {
            var currentStateName = SanitizeStateName(currentState.Name);
            diagram.AppendLine($"    %% Highlight current state");
            diagram.AppendLine($"    style {currentStateName} fill:#f9f,stroke:#333,stroke-width:2px");
        }

        var globalTriggers = GetGlobalTriggers().Select(t => t.Name).Distinct().OrderBy(n => n).ToList();
        if (globalTriggers.Count > 0)
        {
            diagram.AppendLine();
            diagram.AppendLine("%% Global Triggers (no state change)");
            foreach (var triggerName in globalTriggers)
            {
                diagram.AppendLine($"%% - {triggerName}");
            }
        }

        return diagram.ToString();
    }

    /// <summary>
    /// Returns all triggers that have no state change (null→null transitions).
    /// These are available in all states and are excluded from the flow diagram.
    /// </summary>
    public IEnumerable<StateMachineTrigger> GetGlobalTriggers()
    {
        var globalTriggerIds = _transitions
            .Where(t => !t.FromStateId.HasValue && !t.ToStateId.HasValue)
            .Select(t => t.TriggerId)
            .ToHashSet();

        return _triggers.Where(t => globalTriggerIds.Contains(t.Id));
    }

    /// <summary>
    /// Sanitizes state names for Mermaid diagram compatibility.
    /// </summary>
    private static string SanitizeStateName(string stateName)
    {
        // Replace spaces and special characters with underscores
        return System.Text.RegularExpressions.Regex.Replace(stateName, @"[^a-zA-Z0-9]", "_");
    }

    /// <summary>
    /// Gets a human-readable description of a requirement.
    /// </summary>
    private static string GetRequirementDescription(IStateMachineTransitionRequirement requirement)
    {
        // Try to get description from the requirement itself
        if (requirement is StateMachineTransitionRequirement baseRequirement &&
            !string.IsNullOrEmpty(baseRequirement.Description))
        {
            return baseRequirement.Description;
        }

        // Fall back to type name
        return requirement.GetType().Name.Replace("Requirement", "").Replace("_", " ");
    }

    /// <summary>
    /// Validates the definition for completeness and consistency.
    /// </summary>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        // Check if we have at least one state
        if (!_states.Any())
            errors.Add("Definition must have at least one state.");

        // Check if initial state exists
        if (InitialState == null)
            errors.Add("Initial state is not defined.");

        // Check for orphaned states (states with no transitions leading to them, except initial state)
        var reachableStates = new HashSet<Guid>();
        if (InitialState != null)
            reachableStates.Add(InitialState.Id);

        foreach (var transition in _transitions)
        {
            if (transition.ToStateId.HasValue)
                reachableStates.Add(transition.ToStateId.Value);
        }

        var orphanedStates = InitialState != null
            ? _states.Where(s => s.Id != InitialState.Id && !reachableStates.Contains(s.Id)).ToList()
            : _states.Where(s => !reachableStates.Contains(s.Id)).ToList();
        if (orphanedStates.Any())
        {
            errors.Add($"Orphaned states found: {string.Join(", ", orphanedStates.Select(s => s.Name))}");
        }

        // Check for unused triggers
        var usedTriggers = _transitions.Select(t => t.TriggerId).ToHashSet();
        var unusedTriggers = _triggers.Where(t => !usedTriggers.Contains(t.Id)).ToList();
        if (unusedTriggers.Any())
        {
            errors.Add($"Unused triggers found: {string.Join(", ", unusedTriggers.Select(t => t.Name))}");
        }

        return errors;
    }

    /// <summary>
    /// Sets the mapping from previous-version state IDs to this version's state IDs.
    /// Only allowed on Draft definitions.
    /// </summary>
    public void SetPreviousVersionStateMapping(IReadOnlyDictionary<Guid, Guid> mapping)
    {
        if (Status != StateMachineDefinitionStatus.Draft)
            throw new InvalidOperationException("State mapping can only be set on draft definitions.");

        PreviousVersionStateMapping = mapping is null ? [] : new Dictionary<Guid, Guid>(mapping);
    }

    /// <summary>
    /// Validates that <see cref="PreviousVersionStateMapping"/> is complete and consistent against
    /// <paramref name="previousVersion"/>.
    /// Rules:
    ///   - Every state in <paramref name="previousVersion"/> must have an entry in the mapping.
    ///   - Every mapped target state ID must exist in this definition.
    ///   - Multiple previous-version states may map to the same target (many-to-one allowed).
    /// </summary>
    public IEnumerable<string> ValidatePreviousVersionMapping(StateMachineDefinition previousVersion)
    {
        if (previousVersion is null)
        {
            yield return "Previous version definition is required.";
            yield break;
        }

        var currentStateIds = States.Select(s => s.Id).ToHashSet();

        foreach (var state in previousVersion.States)
        {
            if (!PreviousVersionStateMapping.ContainsKey(state.Id))
                yield return $"Previous version state '{state.Name}' (ID: {state.Id}) has no mapping entry.";
        }

        foreach (var (_, targetStateId) in PreviousVersionStateMapping)
        {
            if (!currentStateIds.Contains(targetStateId))
                yield return $"Mapped target state ID '{targetStateId}' does not exist in this definition.";
        }
    }

    public void SetStatus(StateMachineDefinitionStatus status)
    {
        if (status == StateMachineDefinitionStatus.Published)
        {
            var errors = Validate().ToList();
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Cannot publish state machine definition with validation errors: {string.Join("; ", errors)}");
        }

        Status = status;
    }
}


public class StateMachineDefinition<TEntity> : StateMachineDefinition where TEntity : IEntity
{
    public Guid EntityId { get; private set; }
    public TEntity? Entity { get; private set; }

    // EF Core constructor
    protected StateMachineDefinition() : base() { }

    protected StateMachineDefinition(
        TEntity entity,
        string initialStateName,
        int version = 1)
        : base(initialStateName, version)
    {
        EntityId = entity.Id;
    }

    public void SetEntity(TEntity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        EntityId = entity.Id;
    }



    /// <summary>
    /// Creates a new version of this definition.
    /// </summary>
    public override StateMachineDefinition CreateNewVersion(int version)
    {
        if (InitialState == null)
            throw new InvalidOperationException("Cannot create new version without an initial state.");

        var newDefinition = new StateMachineDefinition<TEntity>(Entity!, InitialState.Name, version);

        CopyDefinitionDataTo(newDefinition);

        return newDefinition;
    }
}

