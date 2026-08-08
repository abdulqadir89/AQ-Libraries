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

    /// <summary>
    /// Maximum number of search results to return
    /// </summary>
    int? MaxResults { get; set; }
}

/// <summary>
/// Request model for searchable queries
/// </summary>
public class SearchableRequest : ISearchableRequest
{
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

        if (request.MaxResults.HasValue)
        {
            specification.MaxResults = request.MaxResults;
        }

        return query.ApplySearch(specification);
    }
}
