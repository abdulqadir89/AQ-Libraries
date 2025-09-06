using AQ.Utilities.Results;

namespace AQ.CQRS.Query;

/// <summary>
/// Provides methods for dispatching queries to their appropriate handlers.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatches a query to its handler and returns the result.
    /// </summary>
    /// <typeparam name="TQuery">The type of query to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="query">The query to dispatch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result<TResult>> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;
}
