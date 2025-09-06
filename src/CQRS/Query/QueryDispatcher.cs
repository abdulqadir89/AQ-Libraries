using AQ.Utilities.Results;
using Microsoft.Extensions.Logging;

namespace AQ.CQRS.Query;

/// <summary>
/// Implementation of query dispatcher using dependency injection to resolve handlers.
/// </summary>
public class QueryDispatcher(IServiceProvider serviceProvider, ILogger<QueryDispatcher> logger) : IQueryDispatcher
{
    /// <summary>
    /// Dispatches a query to its handler and returns the result.
    /// </summary>
    /// <typeparam name="TQuery">The type of query to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="query">The query to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    public async Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = typeof(TQuery);
        var resultType = typeof(TResult);
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, resultType);

        logger.LogDebug("Dispatching query {QueryType} with result type {ResultType}", queryType.Name, resultType.Name);

        var handler = serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            logger.LogError("No handler found for query {QueryType}", queryType.Name);
            return Result.Failure<TResult>(Error.NotFound("Handler.NotFound", $"No handler found for query {queryType.Name}"));
        }

        try
        {
            var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle));
            if (handleMethod is not null)
            {
                var task = (Task<Result<TResult>>?)handleMethod.Invoke(handler, [query, cancellationToken]);
                if (task is not null)
                {
                    var result = await task;
                    logger.LogDebug("Successfully processed query {QueryType}", queryType.Name);
                    return result;
                }
            }

            logger.LogError("Handle method not found for query {QueryType}", queryType.Name);
            return Result.Failure<TResult>(Error.NotFound("Handler.MethodNotFound", $"Handle method not found for query {queryType.Name}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling query {QueryType}", queryType.Name);
            // Re-throw exceptions as they represent infrastructure/technical errors, not business validation failures
            throw;
        }
    }
}
