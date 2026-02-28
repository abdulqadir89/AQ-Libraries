using Microsoft.Extensions.DependencyInjection;

namespace AQ.StateMachine.Services;

public static class StateMachineServiceCollectionExtensions
{
    /// <summary>
    /// Registers state machine evaluation and execution services for dependency injection.
    /// Implementation projects should handle their own orchestration logic.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The updated service collection</returns>
    public static IServiceCollection AddStateMachineServices(this IServiceCollection services)
    {
        services.AddScoped<IStateMachineRequirementEvaluationService, StateMachineRequirementEvaluationService>();
        services.AddScoped<IStateMachineEffectExecutionService, StateMachineEffectExecutionService>();
        return services;
    }
}

