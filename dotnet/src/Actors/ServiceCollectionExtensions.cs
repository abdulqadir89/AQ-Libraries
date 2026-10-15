using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AQ.Actors;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IActorContext"/> (singleton) and <see cref="IActorScopeFactory"/>.</summary>
    // RULE ACT-6
    public static IServiceCollection AddAqActors(this IServiceCollection services)
    {
        services.TryAddSingleton<IActorContext, ActorContext>();
        services.TryAddSingleton<IActorScopeFactory, ActorScopeFactory>();
        return services;
    }
}
