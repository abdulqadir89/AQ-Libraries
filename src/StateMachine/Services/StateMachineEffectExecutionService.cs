using AQ.StateMachine.Entities;
using System.Diagnostics;
using System.Reflection;

namespace AQ.StateMachine.Services;

/// <summary>
/// Configuration options for the state machine effect execution service.
/// </summary>
public class StateMachineEffectExecutionOptions
{
    /// <summary>
    /// Assemblies to scan for handlers. If empty, will scan the calling assembly.
    /// </summary>
    public List<Assembly> HandlerAssemblies { get; set; } = new();

    /// <summary>
    /// Whether to auto-register handlers found in assemblies.
    /// </summary>
    public bool AutoRegisterHandlers { get; set; } = true;

    /// <summary>
    /// Whether to continue executing remaining effects if one fails.
    /// </summary>
    public bool ContinueOnFailure { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for all effects to execute.
    /// </summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Concrete implementation of the state machine effect execution service.
/// Automatically registers all handlers found in configured assemblies.
/// Supports multiple generic handlers that all get executed.
/// </summary>
public class StateMachineEffectExecutionService : IStateMachineEffectExecutionService
{
    private readonly Dictionary<Type, List<object>> _specificHandlers = new();
    private readonly List<IStateMachineTransitionEffectHandler> _genericHandlers = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly StateMachineEffectExecutionOptions _options;

    public StateMachineEffectExecutionService(
        IServiceProvider serviceProvider,
        StateMachineEffectExecutionOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? new StateMachineEffectExecutionOptions();

        if (_options.AutoRegisterHandlers)
        {
            RegisterAllHandlers();
        }
    }

    public async Task<EffectExecutionSummary> ExecuteEffectsAsync(
        IEnumerable<IStateMachineTransitionEffect> effects,
        StateMachineInstance stateMachine,
        StateMachineTransitionInfo transitionInfo)
    {
        var effectsList = effects.ToList();
        var executionStatuses = new List<EffectExecutionStatus>();
        var stopwatch = Stopwatch.StartNew();

        // Sort effects by execution order
        var sortedEffects = effectsList
            .OrderBy(e => e is StateMachineTransitionEffect ste ? ste.ExecutionOrder : 0)
            .ToList();

        // Phase 1: Execute using specific handlers
        foreach (var effect in sortedEffects)
        {
            var effectType = effect.GetType();
            var status = new EffectExecutionStatus
            {
                Effect = effect,
                IsExecuted = false,
                WasProcessedBySpecificHandler = false
            };

            var effectStopwatch = Stopwatch.StartNew();

            if (_specificHandlers.TryGetValue(effectType, out var handlers))
            {
                // Execute all handlers for this effect type
                foreach (var handler in handlers)
                {
                    try
                    {
                        var handlerType = typeof(IStateMachineTransitionEffectHandler<>).MakeGenericType(effectType);
                        var handleMethod = handlerType.GetMethod("HandleAsync");

                        if (handleMethod != null)
                        {
                            // Convert domain TransitionExecutionInfo to application StateMachineTransitionInfo
                            var domainTransitionInfo = new TransitionExecutionInfo
                            {
                                StateMachineId = stateMachine.Id,
                                PreviousStateId = transitionInfo.PreviousStateId,
                                NewStateId = transitionInfo.NewStateId ?? Guid.Empty,
                                TriggerId = transitionInfo.TriggerId,
                                WasForced = transitionInfo.WasForced,
                                Reason = transitionInfo.Reason,
                                TransitionedAt = transitionInfo.TransitionedAt
                            };

                            var task = (Task<bool>)handleMethod.Invoke(handler, [effect, stateMachine, domainTransitionInfo])!;
                            var result = await task;

                            if (result)
                            {
                                status.IsExecuted = true;
                                status.WasProcessedBySpecificHandler = true;
                                status.HandlerUsed = handler.GetType().Name;
                                status.ExecutedAt = DateTimeOffset.UtcNow;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        status.FailureReason = ex.Message;
                        if (!_options.ContinueOnFailure)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                // No specific handler found - mark as not processed by specific handler
                status.FailureReason = $"No specific handler found for effect type {effectType.Name}";
            }

            effectStopwatch.Stop();
            status.ExecutionDuration = effectStopwatch.Elapsed;
            executionStatuses.Add(status);

            // Stop if we're not continuing on failure and this effect failed
            if (!_options.ContinueOnFailure && !status.IsExecuted && !IsOptionalEffect(effect))
            {
                break;
            }
        }

        // Phase 2: Process with all generic handlers
        foreach (var genericHandler in _genericHandlers)
        {
            try
            {
                // Convert domain TransitionExecutionInfo to application StateMachineTransitionInfo
                var domainTransitionInfo = new TransitionExecutionInfo
                {
                    StateMachineId = stateMachine.Id,
                    PreviousStateId = transitionInfo.PreviousStateId,
                    NewStateId = transitionInfo.NewStateId ?? Guid.Empty,
                    TriggerId = transitionInfo.TriggerId,
                    WasForced = transitionInfo.WasForced,
                    Reason = transitionInfo.Reason,
                    TransitionedAt = transitionInfo.TransitionedAt
                };

                var updatedStatuses = await genericHandler.HandleAsync(executionStatuses, stateMachine, domainTransitionInfo);
                executionStatuses = updatedStatuses.ToList();
            }
            catch
            {
                // Log generic handler failures but continue
                // In a real implementation, you'd use proper logging here
            }
        }

        stopwatch.Stop();

        var successfulEffects = executionStatuses.Count(s => s.IsExecuted);
        var failedEffects = executionStatuses.Count(s => !s.IsExecuted && !IsOptionalEffect(s.Effect));
        var allEffectsExecuted = failedEffects == 0;

        var failureReasons = executionStatuses
            .Where(s => !s.IsExecuted && !string.IsNullOrEmpty(s.FailureReason) && !IsOptionalEffect(s.Effect))
            .Select(s => s.FailureReason!)
            .ToList();

        return new EffectExecutionSummary
        {
            AllEffectsExecuted = allEffectsExecuted,
            EffectResults = executionStatuses,
            FailureReasons = failureReasons,
            TotalEffects = effectsList.Count,
            SuccessfulEffects = successfulEffects,
            FailedEffects = failedEffects,
            TotalExecutionTime = stopwatch.Elapsed
        };
    }

    private static bool IsOptionalEffect(IStateMachineTransitionEffect effect)
    {
        return effect is StateMachineTransitionEffect ste && ste.IsOptional;
    }

    public void RegisterSpecificHandler<TEffect>(IStateMachineTransitionEffectHandler<TEffect> handler)
        where TEffect : IStateMachineTransitionEffect
    {
        var effectType = typeof(TEffect);

        if (!_specificHandlers.ContainsKey(effectType))
        {
            _specificHandlers[effectType] = new List<object>();
        }

        _specificHandlers[effectType].Add(handler);
    }

    public void RegisterGenericHandler(IStateMachineTransitionEffectHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // Allow multiple generic handlers
        _genericHandlers.Add(handler);
    }

    public IEnumerable<EffectHandlerInfo> GetSpecificHandlersForEffectType<TEffect>()
        where TEffect : IStateMachineTransitionEffect
    {
        return GetSpecificHandlersForEffectType(typeof(TEffect));
    }

    public IEnumerable<EffectHandlerInfo> GetSpecificHandlersForEffectType(Type effectType)
    {
        if (!_specificHandlers.TryGetValue(effectType, out var handlers))
        {
            return Enumerable.Empty<EffectHandlerInfo>();
        }

        return handlers.Select(h => new EffectHandlerInfo
        {
            EffectTypeName = effectType.Name,
            HandlerType = h.GetType().Name,
            IsGeneric = false,
            Description = $"Handler for {effectType.Name}"
        });
    }

    public IEnumerable<EffectHandlerInfo> GetGenericHandlers()
    {
        return _genericHandlers.Select(h => new EffectHandlerInfo
        {
            EffectTypeName = "*",
            HandlerType = h.GetType().Name,
            IsGeneric = true,
            Description = "Generic handler for all effects"
        });
    }

    /// <summary>
    /// Adds an assembly to scan for handlers.
    /// </summary>
    /// <param name="assembly">Assembly to scan</param>
    public void AddHandlerAssembly(Assembly assembly)
    {
        if (!_options.HandlerAssemblies.Contains(assembly))
        {
            _options.HandlerAssemblies.Add(assembly);

            if (_options.AutoRegisterHandlers)
            {
                RegisterHandlersFromAssembly(assembly);
            }
        }
    }

    /// <summary>
    /// Automatically registers all handlers found in configured assemblies.
    /// </summary>
    private void RegisterAllHandlers()
    {
        var assembliesToScan = _options.HandlerAssemblies.Any()
            ? _options.HandlerAssemblies
            : new List<Assembly> { Assembly.GetCallingAssembly() };

        foreach (var assembly in assembliesToScan)
        {
            RegisterHandlersFromAssembly(assembly);
        }
    }

    private void RegisterHandlersFromAssembly(Assembly assembly)
    {
        // Register specific handlers
        RegisterSpecificHandlersFromAssembly(assembly);

        // Register generic handlers
        RegisterGenericHandlersFromAssembly(assembly);
    }

    private void RegisterSpecificHandlersFromAssembly(Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IStateMachineTransitionEffectHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = _serviceProvider.GetService(handlerType) ?? Activator.CreateInstance(handlerType);
                if (handler != null)
                {
                    var interfaceType = handlerType.GetInterfaces()
                        .First(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IStateMachineTransitionEffectHandler<>));

                    var effectType = interfaceType.GetGenericArguments()[0];

                    if (!_specificHandlers.ContainsKey(effectType))
                    {
                        _specificHandlers[effectType] = new List<object>();
                    }

                    _specificHandlers[effectType].Add(handler);
                }
            }
            catch
            {
                // Skip handlers that can't be instantiated
            }
        }
    }

    private void RegisterGenericHandlersFromAssembly(Assembly assembly)
    {
        var genericHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(IStateMachineTransitionEffectHandler).IsAssignableFrom(t))
            .ToList();

        // Register all found generic handlers
        foreach (var genericHandlerType in genericHandlerTypes)
        {
            try
            {
                var handler = _serviceProvider.GetService(genericHandlerType) as IStateMachineTransitionEffectHandler
                    ?? Activator.CreateInstance(genericHandlerType) as IStateMachineTransitionEffectHandler;

                if (handler != null)
                {
                    _genericHandlers.Add(handler);
                }
            }
            catch
            {
                // Skip handlers that can't be instantiated
            }
        }
    }
}

