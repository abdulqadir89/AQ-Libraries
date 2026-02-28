using AQ.CQRS.Command;
using AQ.CQRS.Query;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AQ.CQRS;

/// <summary>
/// Extension methods for registering CQRS services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds CQRS dispatchers and handlers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for command and query handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Register handlers from specified assemblies
        services.AddCommandHandlers(assemblies);
        services.AddQueryHandlers(assemblies);

        return services;
    }

    /// <summary>
    /// Adds command handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for command handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract &&
                (ImplementsGenericInterface(type, typeof(ICommandHandler<>)) ||
                 ImplementsGenericInterface(type, typeof(ICommandHandler<,>))))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            // Register handlers for ICommandHandler<TCommand> (no result)
            var interfaceTypesNoResult = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));

            foreach (var interfaceType in interfaceTypesNoResult)
            {
                services.AddScoped(interfaceType, handlerType);
            }

            // Register handlers for ICommandHandler<TCommand, TResult> (with result)
            var interfaceTypesWithResult = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

            foreach (var interfaceType in interfaceTypesWithResult)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Adds query handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for query handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQueryHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && ImplementsGenericInterface(type, typeof(IQueryHandler<,>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

            foreach (var interfaceType in interfaceTypes)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }

    private static bool ImplementsGenericInterface(Type type, Type genericInterfaceType)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType);
    }
}
