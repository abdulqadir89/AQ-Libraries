namespace AQ.Utilities.Results.Extensions;

/// <summary>
/// Extension methods for IQueryable to support data retrieval with skip/take
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies skip/take to a queryable and returns a DataResult
    /// This method accepts a delegate to materialize the query, allowing different implementations (EF Core, LINQ to Objects, etc.)
    /// </summary>
    /// <typeparam name="T">The type of the query result</typeparam>
    /// <param name="query">The queryable to process</param>
    /// <param name="skip">The number of items to skip (0-based offset)</param>
    /// <param name="take">The maximum number of items to return</param>
    /// <param name="countAsync">Delegate to count items asynchronously</param>
    /// <param name="toListAsync">Delegate to materialize the query as a list asynchronously</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A DataResult containing the data and metadata</returns>
    public static async Task<DataSet<T>> ToDataResultAsync<T>(
        this IQueryable<T> query,
        int? skip,
        int? take,
        Func<IQueryable<T>, CancellationToken, Task<int>> countAsync,
        Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (skip == null || skip < 0) skip = 0;
        if (take == null || take < 1) take = 50;

        // Get total count before applying skip/take
        var totalCount = await countAsync(query, cancellationToken);

        // Apply skip/take
        var dataQuery = query
            .Skip(skip.Value)
            .Take(take.Value);

        var data = await toListAsync(dataQuery, cancellationToken);

        return DataSet<T>.Create(data, skip.Value, take.Value, totalCount);
    }

    /// <summary>
    /// Applies skip/take to a queryable with an existing count and returns a DataResult
    /// This is useful when you already have the total count to avoid an extra database call
    /// </summary>
    /// <typeparam name="T">The type of the query result</typeparam>
    /// <param name="query">The queryable to process</param>
    /// <param name="skip">The number of items to skip (0-based offset)</param>
    /// <param name="take">The maximum number of items to return</param>
    /// <param name="totalCount">The pre-calculated total count</param>
    /// <param name="toListAsync">Delegate to materialize the query as a list asynchronously</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A DataResult containing the data and metadata</returns>
    public static async Task<DataSet<T>> ToDataResultAsync<T>(
        this IQueryable<T> query,
        int skip,
        int take,
        int totalCount,
        Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (skip < 0) skip = 0;
        if (take < 1) take = 50;

        // Apply skip/take
        var dataQuery = query
            .Skip(skip)
            .Take(take);

        var data = await toListAsync(dataQuery, cancellationToken);

        return DataSet<T>.Create(data, skip, take, totalCount);
    }
}
