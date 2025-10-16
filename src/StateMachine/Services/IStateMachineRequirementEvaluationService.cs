using AQ.StateMachine.Entities;

namespace AQ.StateMachine.Services;

/// <summary>
/// Service for evaluating state machine transition requirements.
/// Evaluates requirements in sequence: specific handlers first, then generic handler.
/// Handlers are responsible for fetching their own data from the database.
/// </summary>
public interface IStateMachineRequirementEvaluationService
{
    /// <summary>
    /// Evaluates all requirements for a transition in the correct sequence.
    /// 1. First evaluates each requirement using specific typed handlers (OR relationship per requirement)
    /// 2. Then passes all requirements with their status to the generic handler
    /// Returns true only if ALL requirements are fulfilled (AND relationship).
    /// </summary>
    /// <param name="requirements">The requirements to evaluate</param>
    /// <param name="stateMachineId">The state machine instance ID</param>
    /// <param name="requirementsContext">Requirements context data where key is requirement type name and value is the context object</param>
    /// <returns>Detailed evaluation result</returns>
    Task<RequirementEvaluationSummary> EvaluateRequirementsAsync(
        IEnumerable<IStateMachineTransitionRequirement> requirements,
        Guid stateMachineId,
        IDictionary<string, object>? requirementsContext = null);

    /// <summary>
    /// Registers a typed requirement handler at runtime.
    /// </summary>
    /// <typeparam name="TRequirement">The requirement type</typeparam>
    /// <param name="handler">The handler instance</param>
    void RegisterSpecificHandler<TRequirement>(IStateMachineTransitionRequirementHandler<TRequirement> handler)
        where TRequirement : IStateMachineTransitionRequirement;

    /// <summary>
    /// Registers a generic requirement handler.
    /// Multiple generic handlers can be registered and will all be evaluated.
    /// </summary>
    /// <param name="handler">The generic handler instance</param>
    void RegisterGenericHandler(IStateMachineTransitionHandler handler);

    /// <summary>
    /// Gets all registered specific handlers for a requirement type.
    /// </summary>
    /// <typeparam name="TRequirement">The requirement type</typeparam>
    /// <returns>Collection of handler information</returns>
    IEnumerable<HandlerInfo> GetSpecificHandlersForRequirementType<TRequirement>()
        where TRequirement : IStateMachineTransitionRequirement;

    /// <summary>
    /// Gets all registered specific handlers for a requirement type by type.
    /// </summary>
    /// <param name="requirementType">The requirement type</param>
    /// <returns>Collection of handler information</returns>
    IEnumerable<HandlerInfo> GetSpecificHandlersForRequirementType(Type requirementType);

    /// <summary>
    /// Gets information about all registered generic handlers.
    /// </summary>
    /// <returns>Collection of handler information</returns>
    IEnumerable<HandlerInfo> GetGenericHandlers();
}

/// <summary>
/// Information about a registered requirement handler.
/// </summary>
public class HandlerInfo
{
    public string RequirementTypeName { get; set; } = default!;
    public string HandlerType { get; set; } = default!;
    public bool IsGeneric { get; set; }
    public string? Description { get; set; }
}

