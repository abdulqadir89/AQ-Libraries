using AQ.Abstractions;
using AQ.Entities;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a reusable state machine definition that contains states, triggers, and transitions.
/// This is the template/blueprint that state machine instances are based on.
/// </summary>
public abstract class StateMachineDefinition : Entity, IHasStatus<StateMachineDefinitionStatus>
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
            // Find the corresponding states and triggers in the new definition
            var newFromState = transition.FromState != null
                ? newDefinition._states.FirstOrDefault(s => s.Name == transition.FromState.Name)
                : null;
            var newToState = transition.ToState != null
                ? newDefinition._states.FirstOrDefault(s => s.Name == transition.ToState.Name)
                : null;
            var newTrigger = newDefinition._triggers.FirstOrDefault(t => t.Name == transition.Trigger!.Name);

            if (newTrigger != null)
            {
                var newTransition = StateMachineTransition.Create(
                    newDefinition,
                    newFromState,
                    newToState,
                    newTrigger,
                    transition.Description,
                    transition.Requirements,
                    transition.Effects);
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
        return GetTransitionsFromState(state).Select(t => t.Trigger!).Distinct();
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
            var fromStateName = transition.FromState != null
                ? SanitizeStateName(transition.FromState.Name)
                : "[*]";
            var toStateName = transition.ToState != null
                ? SanitizeStateName(transition.ToState.Name)
                : "[*]";
            var triggerName = transition.Trigger!.Name;

            diagram.AppendLine($"    {fromStateName} --> {toStateName} : {triggerName}");

            // Add note with requirements and effects if they exist
            if (transition.HasRequirements || transition.HasEffects)
            {
                diagram.AppendLine($"    note right of {toStateName}");

                // Add requirements
                if (transition.HasRequirements)
                {
                    var requirements = transition.Requirements!
                        .Select(r => GetRequirementDescription(r))
                        .Where(desc => !string.IsNullOrEmpty(desc));

                    foreach (var requirement in requirements)
                    {
                        diagram.AppendLine($"      Require: {requirement}");
                    }
                }

                // Add effects
                if (transition.HasEffects)
                {
                    var effects = transition.Effects!
                        .Select(e => GetEffectDescription(e))
                        .Where(desc => !string.IsNullOrEmpty(desc));

                    foreach (var effect in effects)
                    {
                        diagram.AppendLine($"      Effect: {effect}");
                    }
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

        return diagram.ToString();
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
    /// Gets a human-readable description of an effect.
    /// </summary>
    private static string GetEffectDescription(IStateMachineTransitionEffect effect)
    {
        // Try to get description from the effect itself
        if (effect is StateMachineTransitionEffect baseEffect &&
            !string.IsNullOrEmpty(baseEffect.Description))
        {
            return baseEffect.Description;
        }

        // Fall back to type name
        return effect.GetType().Name.Replace("Effect", "").Replace("_", " ");
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

    public void SetStatus(StateMachineDefinitionStatus status)
    {
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
        Entity = entity;
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
        newDefinition.SetEntity(Entity!);

        CopyDefinitionDataTo(newDefinition);

        return newDefinition;
    }
}

