using AQ.Events.Domain;

namespace AQ.Events.Dispatchers;

/// <summary>
/// Runs one handler for one event. The dispatcher calls it once per handler, so a policy (a retry, a transaction)
/// applies to that handler alone and never to the handlers that already ran. Without a registration the handler is
/// called directly.
/// </summary>
public interface IDomainEventHandlerInvoker
{
    Task InvokeAsync(IDomainEventHandler handler, IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
