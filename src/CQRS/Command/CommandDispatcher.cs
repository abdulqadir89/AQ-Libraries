using AQ.Common.Domain.Results;

using Microsoft.Extensions.Logging;

namespace AQ.Common.Application.CQRS;

/// <summary>
/// Implementation of command dispatcher using dependency injection to resolve handlers.
/// </summary>
public class CommandDispatcher(IServiceProvider serviceProvider, ILogger<CommandDispatcher> logger) : ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command to its handler for processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    public async Task<Result> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = typeof(TCommand);
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

        logger.LogDebug("Dispatching command {CommandType}", commandType.Name);

        var handler = serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            logger.LogError("No handler found for command {CommandType}", commandType.Name);
            return Result.Failure(Error.NotFound("Handler.NotFound", $"No handler found for command {commandType.Name}"));
        }

        try
        {
            var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.Handle));
            if (handleMethod is not null)
            {
                var task = (Task<Result>?)handleMethod.Invoke(handler, [command, cancellationToken]);
                if (task is not null)
                {
                    var result = await task;
                    logger.LogDebug("Successfully processed command {CommandType}", commandType.Name);
                    return result;
                }
            }

            logger.LogError("Handle method not found for command {CommandType}", commandType.Name);
            return Result.Failure(Error.NotFound("Handler.MethodNotFound", $"Handle method not found for command {commandType.Name}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling command {CommandType}", commandType.Name);
            // Re-throw exceptions as they represent infrastructure/technical errors, not business validation failures
            throw;
        }
    }

    /// <summary>
    /// Dispatches a command with a result to its handler for processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    public async Task<Result<TResult>> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = typeof(TCommand);
        var resultType = typeof(TResult);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);

        logger.LogDebug("Dispatching command {CommandType} with result type {ResultType}", commandType.Name, resultType.Name);

        var handler = serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            logger.LogError("No handler found for command {CommandType}", commandType.Name);
            return Result.Failure<TResult>(Error.NotFound("Handler.NotFound", $"No handler found for command {commandType.Name}"));
        }

        try
        {
            var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle));
            if (handleMethod is not null)
            {
                var task = (Task<Result<TResult>>?)handleMethod.Invoke(handler, [command, cancellationToken]);
                if (task is not null)
                {
                    var result = await task;
                    logger.LogDebug("Successfully processed command {CommandType}", commandType.Name);
                    return result;
                }
            }

            logger.LogError("Handle method not found for command {CommandType}", commandType.Name);
            return Result.Failure<TResult>(Error.NotFound("Handler.MethodNotFound", $"Handle method not found for command {commandType.Name}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling command {CommandType}", commandType.Name);
            // Re-throw exceptions as they represent infrastructure/technical errors, not business validation failures
            throw;
        }
    }
}
