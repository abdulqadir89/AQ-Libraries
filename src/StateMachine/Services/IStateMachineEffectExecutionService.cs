using AQ.StateMachineEntities;

namespace AQ.StateMachine.Services;

/// <summary>
/// Service for executing state machine transition effects.
/// Executes effects in sequence: specific handlers first, then generic handlers.
/// </summary>
public interface IStateMachineEffectExecutionService
{
    /// <summary>
    /// Executes all effects for a transition after a successful transition.
    /// 1. First executes each effect using specific typed handlers (all handlers are executed)
    /// 2. Then passes all effects with their status to the generic handlers
    /// Non-critical failures are logged but don't prevent the transition.
    /// </summary>
    /// <param name="effects">The effects to execute</param>
    /// <param name="stateMachine">The state machine instance</param>
    /// <param name="transitionInfo">Information about the completed transition</param>
    /// <returns>Detailed execution result</returns>
    Task<EffectExecutionSummary> ExecuteEffectsAsync(
        IEnumerable<IStateMachineTransitionEffect> effects,
        StateMachineInstance stateMachine,
        StateMachineTransitionInfo transitionInfo);

    /// <summary>
    /// Registers a typed effect handler at runtime.
    /// </summary>
    /// <typeparam name="TEffect">The effect type</typeparam>
    /// <param name="handler">The handler instance</param>
    void RegisterSpecificHandler<TEffect>(IStateMachineTransitionEffectHandler<TEffect> handler)
        where TEffect : IStateMachineTransitionEffect;

    /// <summary>
    /// Registers a generic effect handler.
    /// Multiple generic handlers can be registered and will all be executed.
    /// </summary>
    /// <param name="handler">The generic handler instance</param>
    void RegisterGenericHandler(IStateMachineTransitionEffectHandler handler);

    /// <summary>
    /// Gets all registered specific handlers for an effect type.
    /// </summary>
    /// <typeparam name="TEffect">The effect type</typeparam>
    /// <returns>Collection of handler information</returns>
    IEnumerable<EffectHandlerInfo> GetSpecificHandlersForEffectType<TEffect>()
        where TEffect : IStateMachineTransitionEffect;

    /// <summary>
    /// Gets all registered specific handlers for an effect type by type.
    /// </summary>
    /// <param name="effectType">The effect type</param>
    /// <returns>Collection of handler information</returns>
    IEnumerable<EffectHandlerInfo> GetSpecificHandlersForEffectType(Type effectType);

    /// <summary>
    /// Gets information about all registered generic handlers.
    /// </summary>
    /// <returns>Collection of handler information</returns>
    IEnumerable<EffectHandlerInfo> GetGenericHandlers();
}

/// <summary>
/// Information about a registered effect handler.
/// </summary>
public class EffectHandlerInfo
{
    public string EffectTypeName { get; set; } = default!;
    public string HandlerType { get; set; } = default!;
    public bool IsGeneric { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Summary of effect execution for a transition.
/// </summary>
public class EffectExecutionSummary
{
    public bool AllEffectsExecuted { get; set; }
    public IEnumerable<EffectExecutionStatus> EffectResults { get; set; } = [];
    public IEnumerable<string> FailureReasons { get; set; } = [];
    public int TotalEffects { get; set; }
    public int SuccessfulEffects { get; set; }
    public int FailedEffects { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
}
