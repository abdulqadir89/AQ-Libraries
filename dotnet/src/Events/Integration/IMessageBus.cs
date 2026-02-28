namespace AQ.Events.Integration;

/// <summary>
/// Abstraction for message bus operations
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to the message bus
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sends a message to a specific destination
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="destinationAddress">The destination address</param>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync<T>(string destinationAddress, T message, CancellationToken cancellationToken = default) where T : class;
}
