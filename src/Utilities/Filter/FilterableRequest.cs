using System.Linq.Expressions;

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
/// Request model for paginated and filterable queries
/// </summary>
public class PagedFilterableRequest : IFilterableRequest
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
    public string? FilterExpression { get; set; }
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Search term for general text search
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction
    /// </summary>
    public string? SortDirection { get; set; } = "asc";

    /// <summary>
    /// Gets the sort direction as boolean (true = ascending, false = descending)
    /// </summary>
    public bool IsAscending => !string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
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

    /// <summary>
    /// Applies filters and pagination from a paged filterable request to a query
    /// </summary>
    public static IQueryable<T> ApplyFiltersAndPaging<T>(this IQueryable<T> query, PagedFilterableRequest? request)
    {
        if (request == null)
            return query;

        // Apply filters
        query = query.ApplyFilters(request);

        // Apply general search if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // This is a basic implementation - you might want to customize this
            // based on your specific search requirements
            query = query.Where(BuildSearchExpression<T>(request.SearchTerm));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query = request.IsAscending
                ? query.OrderBy(BuildSortExpression<T>(request.SortBy))
                : query.OrderByDescending(BuildSortExpression<T>(request.SortBy));
        }

        // Apply pagination
        query = query.Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize);

        return query;
    }

    private static Expression<Func<T, bool>> BuildSearchExpression<T>(string searchTerm)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var searchValue = searchTerm.ToLower();

        // Get all string properties
        var stringProperties = typeof(T)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead)
            .ToList();

        if (!stringProperties.Any())
        {
            // Return a expression that always returns false if no string properties
            return Expression.Lambda<Func<T, bool>>(Expression.Constant(false), parameter);
        }

        Expression? searchExpression = null;

        foreach (var property in stringProperties)
        {
            var propertyAccess = Expression.Property(parameter, property);
            var toLowerCall = Expression.Call(propertyAccess, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var containsCall = Expression.Call(toLowerCall, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, Expression.Constant(searchValue));

            searchExpression = searchExpression == null
                ? containsCall
                : Expression.OrElse(searchExpression, containsCall);
        }

        return Expression.Lambda<Func<T, bool>>(searchExpression!, parameter);
    }

    private static Expression<Func<T, object>> BuildSortExpression<T>(string sortBy)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, sortBy);
        var converted = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(converted, parameter);
    }
}
