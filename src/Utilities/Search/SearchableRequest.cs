namespace AQ.Utilities.Search;

/// <summary>
/// Interface for request models that support searching
/// </summary>
public interface ISearchableRequest
{
    /// <summary>
    /// Global search term that applies to all searchable fields
    /// </summary>
    string? SearchTerm { get; set; }

    /// <summary>
    /// Search operator to use for the global search
    /// </summary>
    SearchOperator SearchOperator { get; set; }

    /// <summary>
    /// Minimum score threshold for search results (0.0 to 1.0)
    /// </summary>
    double MinScore { get; set; }

    /// <summary>
    /// Whether to enable fuzzy matching
    /// </summary>
    bool EnableFuzzyMatch { get; set; }
}

/// <summary>
/// Request model for paginated and searchable queries
/// </summary>
public class PagedSearchableRequest : ISearchableRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Global search term that applies to all searchable fields
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Search operator to use for the global search
    /// </summary>
    public SearchOperator SearchOperator { get; set; } = SearchOperator.Contains;

    /// <summary>
    /// Minimum score threshold for search results (0.0 to 1.0)
    /// </summary>
    public double MinScore { get; set; } = 0.1;

    /// <summary>
    /// Whether to enable fuzzy matching
    /// </summary>
    public bool EnableFuzzyMatch { get; set; } = true;

    /// <summary>
    /// Maximum number of search results to return
    /// </summary>
    public int? MaxResults { get; set; }

    /// <summary>
    /// Sort field (for secondary sorting after search scoring)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (for secondary sorting after search scoring)
    /// </summary>
    public string? SortDirection { get; set; } = "asc";

    /// <summary>
    /// Gets the sort direction as boolean (true = ascending, false = descending)
    /// </summary>
    public bool IsAscending => !string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Extension methods for searchable requests
/// </summary>
public static class SearchableRequestExtensions
{
    /// <summary>
    /// Gets the search specification from the request
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="request">The searchable request</param>
    /// <returns>A search specification</returns>
    public static SearchSpecification? GetSearchSpecification<T>(this ISearchableRequest request) where T : class
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
            return null;

        return SearchSpecification.Create(
            request.SearchTerm,
            request.SearchOperator,
            request.MinScore);
    }

    /// <summary>
    /// Applies search from a searchable request to a query
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="request">The searchable request</param>
    /// <returns>Search results</returns>
    public static SearchResults<T> ApplySearch<T>(this IQueryable<T> query, ISearchableRequest? request) where T : class
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Return all results with default scoring if no search term
            return new SearchResults<T>
            {
                Results = query.Take(100).Select(item => new SearchResult<T> { Entity = item, Score = 1.0 }).ToList(),
                TotalCount = query.Count()
            };
        }

        var specification = SearchSpecification.Create(
            request.SearchTerm,
            request.SearchOperator,
            request.MinScore);

        specification.EnableFuzzyMatch = request.EnableFuzzyMatch;

        if (request is PagedSearchableRequest pagedRequest && pagedRequest.MaxResults.HasValue)
        {
            specification.MaxResults = pagedRequest.MaxResults;
        }

        return query.ApplySearch(specification);
    }

    /// <summary>
    /// Applies search and pagination from a paged searchable request to a query
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="request">The paged searchable request</param>
    /// <returns>Search results with pagination applied</returns>
    public static SearchResults<T> ApplySearchAndPaging<T>(this IQueryable<T> query, PagedSearchableRequest? request) where T : class
    {
        if (request == null)
        {
            return new SearchResults<T>
            {
                Results = query.Take(50).Select(item => new SearchResult<T> { Entity = item, Score = 1.0 }).ToList(),
                TotalCount = query.Count()
            };
        }

        var searchResults = query.ApplySearch(request);

        // Apply pagination to search results
        var skip = (request.PageNumber - 1) * request.PageSize;
        searchResults.Results = searchResults.Results.Skip(skip).Take(request.PageSize).ToList();

        // Apply secondary sorting if specified and not already sorted by search score
        if (!string.IsNullOrWhiteSpace(request.SortBy) && !string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            searchResults.Results = request.IsAscending
                ? searchResults.Results.OrderBy(r => GetPropertyValue(r.Entity, request.SortBy)).ToList()
                : searchResults.Results.OrderByDescending(r => GetPropertyValue(r.Entity, request.SortBy)).ToList();
        }

        return searchResults;
    }

    private static object? GetPropertyValue(object obj, string propertyPath)
    {
        try
        {
            var properties = propertyPath.Split('.');
            object? current = obj;

            foreach (var prop in properties)
            {
                if (current == null)
                    return null;

                var propertyInfo = current.GetType().GetProperty(prop, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (propertyInfo == null)
                    return null;

                current = propertyInfo.GetValue(current);
            }

            return current;
        }
        catch
        {
            return null;
        }
    }
}
