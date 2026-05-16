namespace AQ.Events.Domain;

/// <summary>
/// Non-generic marker interface for domain event handlers.
/// Used by the dispatcher to invoke handlers without reflection.
///
/// Consuming apps: do not implement this interface directly.
/// Implement <see cref="IDomainEventHandler{TDomainEvent}"/> instead — it inherits this automatically.
/// </summary>
public interface IDomainEventHandler
{
    /// <summary>
    /// Handles a domain event. The concrete implementation receives the strongly-typed event.
    /// </summary>
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a contract for handling a specific domain event type.
///
/// Consuming apps: implement this interface in your event handler classes.
///
/// DbContext / data access:
///   Inject <c>IDbContextFactory&lt;TContext&gt;</c> instead of <c>TContext</c> directly,
///   and create the context at the start of <see cref="HandleAsync"/> with
///   <c>await using var context = await dbContextFactory.CreateDbContextAsync(ct);</c>
///   This guarantees an isolated context per handler invocation regardless of DI scope.
///
/// Registration:
///   Call <c>services.AddDomainEventHandlersFromAssembly(assembly)</c> to auto-register
///   all handlers in an assembly, or <c>services.AddDomainEventHandler&lt;TEvent, THandler&gt;()</c>
///   to register individually.
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);

    // Bridge the non-generic interface to the strongly-typed method.
    async Task IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is TDomainEvent typed)
            await HandleAsync(typed, cancellationToken);
    }
}
