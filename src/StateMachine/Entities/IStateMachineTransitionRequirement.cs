namespace AQ.StateMachine.Entities;

/// <summary>
/// Base interface for all state machine transition requirements.
/// Requirements are stored as JSON in the database and evaluated during transitions.
/// The requirement type is determined by the actual implementation class type.
/// </summary>
public interface IStateMachineTransitionRequirement
{
    // No properties needed - type is determined by actual implementation type
}

/// <summary>
/// Base abstract class for transition requirements with common properties.
/// The requirement type is automatically determined by the implementing class type.
/// </summary>
public abstract record StateMachineTransitionRequirement : IStateMachineTransitionRequirement
{
    public string? Description { get; init; }
    public bool IsOptional { get; init; } = false;

    /// <summary>
    /// Gets the requirement type name based on the actual implementation type.
    /// </summary>
    public virtual string GetRequirementTypeName() => GetType().Name;
}

/// <summary>
/// Handler interface for specific requirement types.
/// Multiple handlers can exist for the same requirement type (OR relationship).
/// </summary>
public interface IStateMachineTransitionRequirementHandler<TRequirement> where TRequirement : IStateMachineTransitionRequirement
{
    /// <summary>
    /// Evaluates if the requirement is fulfilled.
    /// </summary>
    /// <param name="requirement">The requirement to evaluate</param>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="requirementContext">Specific context object for this requirement type (if available)</param>
    /// <returns>True if requirement is fulfilled, false otherwise</returns>
    Task<bool> HandleAsync(TRequirement requirement, StateMachineInstance stateMachine, object? requirementContext);
}

/// <summary>
/// Generic handler interface that can handle all requirements.
/// This handler receives all requirements with their evaluation status and can process them as needed.
/// </summary>
public interface IStateMachineTransitionHandler
{
    /// <summary>
    /// Handles all requirements for a transition. Called after specific handlers have been evaluated.
    /// </summary>
    /// <param name="requirements">All requirements with their current evaluation status</param>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="requirementsContext">Complete requirements context data</param>
    /// <returns>Updated requirements with final evaluation status</returns>
    Task<IEnumerable<RequirementEvaluationStatus>> HandleAsync(
        IEnumerable<RequirementEvaluationStatus> requirements,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext);
}

/// <summary>
/// Represents the evaluation status of a single requirement.
/// </summary>
public class RequirementEvaluationStatus
{
    public IStateMachineTransitionRequirement Requirement { get; set; } = default!;
    public bool IsFulfilled { get; set; }
    public string? FailureReason { get; set; }
    public string? HandlerUsed { get; set; }
    public bool WasProcessedBySpecificHandler { get; set; }
}

