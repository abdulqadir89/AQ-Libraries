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
        services.AddScoped<IStateMachineRequirementEvaluationService, StateMachineRequirementEvaluationService>();
        services.AddScoped<IStateMachineEffectExecutionService, StateMachineEffectExecutionService>();
        services.AddScoped<IStateMachineService, StateMachineService>();
        return services;
    }
}

