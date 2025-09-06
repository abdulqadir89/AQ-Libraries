using System.Reflection;
using AQ.StateMachineEntities;

namespace AQ.StateMachine.Services;

/// <summary>
/// Configuration options for the state machine requirement evaluation service.
/// </summary>
public class StateMachineRequirementEvaluationOptions
{
    /// <summary>
    /// Assemblies to scan for handlers. If empty, will scan the calling assembly.
    /// </summary>
    public List<Assembly> HandlerAssemblies { get; set; } = new();

    /// <summary>
    /// Whether to auto-register handlers found in assemblies.
    /// </summary>
    public bool AutoRegisterHandlers { get; set; } = true;
}

/// <summary>
/// Concrete implementation of the state machine requirement evaluation service.
/// Automatically registers all handlers found in configured assemblies.
/// Supports multiple generic handlers that all get evaluated.
/// </summary>
public class StateMachineRequirementEvaluationService : IStateMachineRequirementEvaluationService
{
    private readonly Dictionary<Type, List<object>> _specificHandlers = new();
    private readonly List<IStateMachineTransitionHandler> _genericHandlers = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly StateMachineRequirementEvaluationOptions _options;

    public StateMachineRequirementEvaluationService(
        IServiceProvider serviceProvider,
        StateMachineRequirementEvaluationOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? new StateMachineRequirementEvaluationOptions();

        if (_options.AutoRegisterHandlers)
        {
            RegisterAllHandlers();
        }
    }

    public async Task<RequirementEvaluationSummary> EvaluateRequirementsAsync(
        IEnumerable<IStateMachineTransitionRequirement> requirements,
        StateMachineInstance stateMachine,
        IDictionary<string, object>? requirementsContext = null)
    {
        var requirementsList = requirements.ToList();
        var evaluationStatuses = new List<RequirementEvaluationStatus>();

        // Phase 1: Evaluate using specific handlers
        foreach (var requirement in requirementsList)
        {
            var requirementType = requirement.GetType();
            var status = new RequirementEvaluationStatus
            {
                Requirement = requirement,
                IsFulfilled = false,
                WasProcessedBySpecificHandler = false
            };

            // Try specific handlers for this requirement type
            if (_specificHandlers.TryGetValue(requirementType, out var handlers))
            {
                // Get specific context for this requirement type
                object? specificContext = null;
                requirementsContext?.TryGetValue(requirementType.Name, out specificContext);

                foreach (var handler in handlers)
                {
                    try
                    {
                        var handlerType = handler.GetType();
                        var handleMethod = handlerType.GetMethod("HandleAsync");

                        if (handleMethod != null)
                        {
                            var task = (Task<bool>)handleMethod.Invoke(handler, [requirement, stateMachine, specificContext])!;
                            var result = await task;

                            if (result)
                            {
                                status.IsFulfilled = true;
                                status.WasProcessedBySpecificHandler = true;
                                status.HandlerUsed = handlerType.Name;
                                break; // OR relationship - first successful handler wins
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        status.FailureReason = $"Handler {handler.GetType().Name} failed: {ex.Message}";
                    }
                }
            }

            evaluationStatuses.Add(status);
        }

        // Phase 2: Process with all generic handlers
        foreach (var genericHandler in _genericHandlers)
        {
            try
            {
                var processedStatuses = await genericHandler.HandleAsync(evaluationStatuses, stateMachine, requirementsContext);
                evaluationStatuses = processedStatuses.ToList();
            }
            catch (Exception ex)
            {
                // Log error but continue - generic handler failure shouldn't break the evaluation
                foreach (var status in evaluationStatuses.Where(s => !s.IsFulfilled))
                {
                    if (string.IsNullOrEmpty(status.FailureReason))
                    {
                        status.FailureReason = $"Generic handler {genericHandler.GetType().Name} failed: {ex.Message}";
                    }
                }
            }
        }

        var allRequirementsMet = evaluationStatuses.All(s => s.IsFulfilled || s.Requirement is StateMachineTransitionRequirement req && req.IsOptional);
        var failureReasons = evaluationStatuses
            .Where(s => !s.IsFulfilled && !string.IsNullOrEmpty(s.FailureReason))
            .Select(s => s.FailureReason!)
            .ToList();

        return new RequirementEvaluationSummary
        {
            AllRequirementsMet = allRequirementsMet,
            RequirementResults = evaluationStatuses,
            FailureReasons = failureReasons
        };
    }

    public void RegisterSpecificHandler<TRequirement>(IStateMachineTransitionRequirementHandler<TRequirement> handler)
        where TRequirement : IStateMachineTransitionRequirement
    {
        var requirementType = typeof(TRequirement);

        if (!_specificHandlers.ContainsKey(requirementType))
        {
            _specificHandlers[requirementType] = new List<object>();
        }

        _specificHandlers[requirementType].Add(handler);
    }

    public void RegisterGenericHandler(IStateMachineTransitionHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // Allow multiple generic handlers
        _genericHandlers.Add(handler);
    }

    public IEnumerable<HandlerInfo> GetSpecificHandlersForRequirementType<TRequirement>()
        where TRequirement : IStateMachineTransitionRequirement
    {
        return GetSpecificHandlersForRequirementType(typeof(TRequirement));
    }

    public IEnumerable<HandlerInfo> GetSpecificHandlersForRequirementType(Type requirementType)
    {
        if (!_specificHandlers.TryGetValue(requirementType, out var handlers))
        {
            return Enumerable.Empty<HandlerInfo>();
        }

        return handlers.Select(h => new HandlerInfo
        {
            RequirementTypeName = requirementType.Name,
            HandlerType = h.GetType().Name,
            IsGeneric = false,
            Description = $"Handler for {requirementType.Name}"
        });
    }

    public IEnumerable<HandlerInfo> GetGenericHandlers()
    {
        return _genericHandlers.Select(h => new HandlerInfo
        {
            RequirementTypeName = "*",
            HandlerType = h.GetType().Name,
            IsGeneric = true,
            Description = "Generic handler for all requirements"
        });
    }

    public HandlerInfo? GetGenericHandler()
    {
        // Return the first generic handler for backward compatibility
        var firstHandler = _genericHandlers.FirstOrDefault();
        if (firstHandler == null)
        {
            return null;
        }

        return new HandlerInfo
        {
            RequirementTypeName = "*",
            HandlerType = firstHandler.GetType().Name,
            IsGeneric = true,
            Description = "Generic handler for all requirements"
        };
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
                i.GetGenericTypeDefinition() == typeof(IStateMachineTransitionRequirementHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = _serviceProvider.GetService(handlerType);
                if (handler != null)
                {
                    // Get the requirement type from the interface
                    var handlerInterface = handlerType.GetInterfaces()
                        .First(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IStateMachineTransitionRequirementHandler<>));

                    var requirementType = handlerInterface.GetGenericArguments()[0];

                    if (!_specificHandlers.ContainsKey(requirementType))
                    {
                        _specificHandlers[requirementType] = new List<object>();
                    }

                    _specificHandlers[requirementType].Add(handler);
                }
            }
            catch (Exception)
            {
                // Ignore registration failures for individual handlers
                // Could log this in a real implementation
            }
        }
    }

    private void RegisterGenericHandlersFromAssembly(Assembly assembly)
    {
        var genericHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(IStateMachineTransitionHandler).IsAssignableFrom(t))
            .ToList();

        // Register all found generic handlers
        foreach (var genericHandlerType in genericHandlerTypes)
        {
            try
            {
                var handler = _serviceProvider.GetService(genericHandlerType) as IStateMachineTransitionHandler;
                if (handler != null)
                {
                    RegisterGenericHandler(handler);
                }
            }
            catch (Exception)
            {
                // Ignore registration failures
            }
        }
    }
}
