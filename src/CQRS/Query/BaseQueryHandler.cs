using AQ.Common.Domain.Results;

namespace AQ.Common.Application.CQRS;

/// <summary>
/// Base class for query handlers that provides common validation and error handling patterns.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public abstract class BaseQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the query and returns a result.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    public async Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await ValidateAsync(query, cancellationToken);
            if (validationResult.IsFailure)
                return Result.Failure<TResult>(validationResult.Error);

            return await HandleAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the exception and re-throw as exceptions represent infrastructure/technical errors
            // Business validation errors should be returned as Result.Failure
            throw new InvalidOperationException($"Error handling query {typeof(TQuery).Name}", ex);
        }
    }

    /// <summary>
    /// Validates the query before handling.
    /// </summary>
    /// <param name="query">The query to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the validation result.</returns>
    protected virtual Task<Result> ValidateAsync(TQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Handles the validated query.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation result.</returns>
    protected abstract Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
