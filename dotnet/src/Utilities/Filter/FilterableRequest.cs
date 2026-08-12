namespace AQ.Utilities.Filter;

/// <summary>
/// Interface for request models that support filtering
/// </summary>
public interface IFilterableRequest
{
    /// <summary>
    /// Dynamic filter expression with logical operators and parentheses
    /// Examples: 
    /// - Simple: "Name,contains,John"
    /// - Multiple with AND: "Name,contains,John && Age,gt,25 && IsActive,eq,true"
    /// - Complex with OR and grouping: "(Name,contains,John && Age,gt,25) || (Department,eq,IT)"
    /// Format: "PropertyPath,Operator,Value"
    /// Supported operators: eq, ne, gt, gte, lt, lte, contains, startswith, endswith, isnull, isnotnull, in, between
    /// </summary>
    string? FilterExpression { get; set; }
}

/// <summary>
/// Extension methods for filterable requests
/// </summary>
public static class FilterableRequestExtensions
{
    /// <summary>
    /// Gets the parsed filter specification from the request
    /// </summary>
    public static FilterSpecification? GetFilterSpecification(this IFilterableRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.FilterExpression))
        {
            return FilterExpressionParser.ParseComplexExpression(request.FilterExpression);
        }

        return null;
    }

    /// <summary>
    /// Applies filters from a filterable request to a query
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IFilterableRequest? request)
    {
        if (request == null)
            return query;

        var filterSpec = request.GetFilterSpecification();
        return query.ApplyFilter(filterSpec);
    }
}
