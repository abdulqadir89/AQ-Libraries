using AQ.Utilities.Sort;

namespace AQ.CQRS.Query;

/// <summary>
/// Base query for core application queries
/// </summary>
/// <typeparam name="TResult">The type of the query result</typeparam>
public abstract record BaseQuery<TResult> : IQuery<TResult>
{
    // Add common properties for core queries here
}

/// <summary>
/// Base query for paginated and filtered queries that return collections
/// </summary>
/// <typeparam name="TResult">The type of the query result (usually PagedResult{T})</typeparam>
public abstract record BaseGetAllQuery<TResult> : BaseQuery<TResult>, ISortableRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Dynamic filter expression with logical operators and parentheses
    /// Examples: 
    /// - Simple: "Name,contains,IT"
    /// - Multiple with AND: "Name,contains,IT && Level,gt,1 && IsActive,eq,true"
    /// - Complex with OR and grouping: "(Name,contains,IT && Level,gt,1) || (Parent.Name,eq,Engineering)"
    /// Format: "PropertyPath,Operator,Value"
    /// Supported operators: eq, ne, gt, gte, lt, lte, contains, startswith, endswith, isnull, isnotnull, in, between
    /// </summary>
    public string? FilterExpression { get; init; }

    /// <summary>
    /// Search term for general text search
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Complex sort expression supporting multiple fields with directions
    /// Examples:
    /// - Simple: "Name,asc"
    /// - Multiple: "Parent.Name,asc;Description,desc;CreatedDate,desc"
    /// Format: "PropertyPath,Direction;PropertyPath,Direction"
    /// Supported directions: asc, desc, ascending, descending
    /// </summary>
    public string? SortExpression { get; init; }
}
