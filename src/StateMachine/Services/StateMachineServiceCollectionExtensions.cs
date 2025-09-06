using Microsoft.Extensions.DependencyInjection;

namespace AQ.StateMachine.Services;

public static class StateMachineServiceCollectionExtensions
{
    /// <summary>
    /// Registers all state machine services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The updated service collection</returns>
    public static IServiceCollection AddStateMachineServices(this IServiceCollection services)
    {
        services.AddSingleton<IStateMachineRequirementEvaluationService, StateMachineRequirementEvaluationService>();
        services.AddSingleton<IStateMachineEffectExecutionService, StateMachineEffectExecutionService>();
        // Note: Transition service is abstract, so registration should be for a concrete implementation in the application layer.
        // Example: services.AddScoped<IStateMachineTransitionService, MyStateMachineTransitionService>();
        return services;
    }
}
