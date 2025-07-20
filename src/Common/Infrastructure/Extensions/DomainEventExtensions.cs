using AQ.Common.Domain.Events;
using AQ.Common.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AQ.Common.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring domain event handling in the infrastructure layer.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Adds immediate domain event processing (synchronous, in-memory).
    /// Use this for simple scenarios where you want domain events processed immediately
    /// within the same transaction as the business operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddImmediateDomainEventProcessing(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Adds outbox pattern domain event processing (reliable, transactional).
    /// Use this for production scenarios where you need guaranteed event delivery
    /// and eventual consistency. Events are stored in the database and processed
    /// by a background service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOutboxDomainEventProcessing(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, OutboxDomainEventDispatcher>();
        services.AddSingleton<IHostedService, OutboxEventProcessorService>();
        return services;
    }

    /// <summary>
    /// Adds a domain event handler to the dependency injection container.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
    /// <typeparam name="THandler">The handler implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<IDomainEventHandler<TEvent>, THandler>();
        return services;
    }

    /// <summary>
    /// Adds multiple domain event handlers from the specified assembly.
    /// Scans the assembly for all implementations of IDomainEventHandler&lt;T&gt; and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainEventHandlersFromAssembly(this IServiceCollection services, System.Reflection.Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(i => new { HandlerType = t, InterfaceType = i }))
            .ToList();

        foreach (var handlerInfo in handlerTypes)
        {
            services.AddScoped(handlerInfo.InterfaceType, handlerInfo.HandlerType);
        }

        return services;
    }
}
