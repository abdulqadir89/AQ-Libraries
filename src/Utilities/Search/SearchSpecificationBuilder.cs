namespace AQ.Utilities.Search;

/// <summary>
/// Fluent builder for search specifications
/// </summary>
/// <typeparam name="T">The type of entities being searched</typeparam>
public class SearchSpecificationBuilder<T> where T : class
{
    private readonly IQueryable<T> _query;
    private readonly SearchSpecification _specification;
    private readonly Stack<SearchGroup> _groupStack;
    private SearchGroup _currentGroup;

    internal SearchSpecificationBuilder(IQueryable<T> query)
    {
        _query = query;
        _specification = new SearchSpecification();
        _currentGroup = _specification.RootGroup;
        _groupStack = new Stack<SearchGroup>();
    }

    /// <summary>
    /// Sets the global search term
    /// </summary>
    /// <param name="searchTerm">The term to search for across all searchable fields</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> GlobalSearch(string searchTerm)
    {
        _specification.GlobalSearchTerm = searchTerm;
        return this;
    }

    /// <summary>
    /// Adds a search condition for a specific field
    /// </summary>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="searchOperator">The search operator to use</param>
    /// <param name="weight">Weight for this condition</param>
    /// <param name="caseSensitive">Whether the search should be case-sensitive</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> Search(string propertyPath, string searchTerm, SearchOperator searchOperator = SearchOperator.Contains, double weight = 1.0, bool caseSensitive = false)
    {
        _currentGroup.Conditions.Add(new SearchCondition
        {
            PropertyPath = propertyPath,
            SearchTerm = searchTerm,
            Operator = searchOperator,
            Weight = weight,
            CaseSensitive = caseSensitive
        });
        return this;
    }

    /// <summary>
    /// Adds an exact match condition
    /// </summary>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="weight">Weight for this condition</param>
    /// <param name="caseSensitive">Whether the search should be case-sensitive</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> Exact(string propertyPath, string searchTerm, double weight = 1.0, bool caseSensitive = false)
    {
        return Search(propertyPath, searchTerm, SearchOperator.Exact, weight, caseSensitive);
    }

    /// <summary>
    /// Adds a contains condition
    /// </summary>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="weight">Weight for this condition</param>
    /// <param name="caseSensitive">Whether the search should be case-sensitive</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> Contains(string propertyPath, string searchTerm, double weight = 1.0, bool caseSensitive = false)
    {
        return Search(propertyPath, searchTerm, SearchOperator.Contains, weight, caseSensitive);
    }

    /// <summary>
    /// Adds a starts with condition
    /// </summary>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="weight">Weight for this condition</param>
    /// <param name="caseSensitive">Whether the search should be case-sensitive</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> StartsWith(string propertyPath, string searchTerm, double weight = 1.0, bool caseSensitive = false)
    {
        return Search(propertyPath, searchTerm, SearchOperator.StartsWith, weight, caseSensitive);
    }

    /// <summary>
    /// Adds an ends with condition
    /// </summary>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="weight">Weight for this condition</param>
    /// <param name="caseSensitive">Whether the search should be case-sensitive</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> EndsWith(string propertyPath, string searchTerm, double weight = 1.0, bool caseSensitive = false)
    {
        return Search(propertyPath, searchTerm, SearchOperator.EndsWith, weight, caseSensitive);
    }

    /// <summary>
    /// Adds a fuzzy match condition
    /// </summary>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="weight">Weight for this condition</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0)</param>
    /// <param name="maxEditDistance">Maximum edit distance for fuzzy matching</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> Fuzzy(string propertyPath, string searchTerm, double weight = 1.0, double minSimilarity = 0.6, int maxEditDistance = 2)
    {
        _currentGroup.Conditions.Add(new SearchCondition
        {
            PropertyPath = propertyPath,
            SearchTerm = searchTerm,
            Operator = SearchOperator.Fuzzy,
            Weight = weight,
            MinSimilarity = minSimilarity,
            MaxEditDistance = maxEditDistance
        });
        return this;
    }

    /// <summary>
    /// Sets the match type for the current group (Any/All/BestMatch)
    /// </summary>
    /// <param name="matchType">The match type to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> MatchType(SearchMatchType matchType)
    {
        _currentGroup.MatchType = matchType;
        return this;
    }

    /// <summary>
    /// Sets the match type to Any (OR logic)
    /// </summary>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> Any()
    {
        return MatchType(SearchMatchType.Any);
    }

    /// <summary>
    /// Sets the match type to All (AND logic)
    /// </summary>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> All()
    {
        return MatchType(SearchMatchType.All);
    }

    /// <summary>
    /// Sets the match type to BestMatch
    /// </summary>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> BestMatch()
    {
        return MatchType(SearchMatchType.BestMatch);
    }

    /// <summary>
    /// Starts a new search group
    /// </summary>
    /// <param name="matchType">The match type for the new group</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> BeginGroup(SearchMatchType matchType = SearchMatchType.Any)
    {
        _groupStack.Push(_currentGroup);
        var newGroup = new SearchGroup { MatchType = matchType };
        _currentGroup.Groups.Add(newGroup);
        _currentGroup = newGroup;
        return this;
    }

    /// <summary>
    /// Ends the current search group
    /// </summary>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> EndGroup()
    {
        if (_groupStack.Count > 0)
        {
            _currentGroup = _groupStack.Pop();
        }
        return this;
    }

    /// <summary>
    /// Negates the current group (NOT logic)
    /// </summary>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> Not()
    {
        _currentGroup.IsNegated = true;
        return this;
    }

    /// <summary>
    /// Sets the minimum score threshold
    /// </summary>
    /// <param name="minScore">Minimum score (0.0 to 1.0)</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> MinScore(double minScore)
    {
        _specification.MinScore = minScore;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of results
    /// </summary>
    /// <param name="maxResults">Maximum number of results</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> MaxResults(int maxResults)
    {
        _specification.MaxResults = maxResults;
        return this;
    }

    /// <summary>
    /// Enables or disables fuzzy matching globally
    /// </summary>
    /// <param name="enableFuzzyMatch">Whether to enable fuzzy matching</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> EnableFuzzyMatch(bool enableFuzzyMatch = true)
    {
        _specification.EnableFuzzyMatch = enableFuzzyMatch;
        return this;
    }

    /// <summary>
    /// Enables or disables result highlighting
    /// </summary>
    /// <param name="enableHighlighting">Whether to enable highlighting</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> EnableHighlighting(bool enableHighlighting = true)
    {
        _specification.EnableHighlighting = enableHighlighting;
        return this;
    }

    /// <summary>
    /// Sets the ranking algorithm
    /// </summary>
    /// <param name="algorithm">The ranking algorithm to use</param>
    /// <returns>The builder instance for method chaining</returns>
    public SearchSpecificationBuilder<T> RankingAlgorithm(SearchRankingAlgorithm algorithm)
    {
        _specification.RankingAlgorithm = algorithm;
        return this;
    }

    /// <summary>
    /// Builds the search specification and executes the search
    /// </summary>
    /// <returns>Search results</returns>
    public SearchResults<T> Build()
    {
        return _query.ApplySearch(_specification);
    }

    /// <summary>
    /// Gets the search specification without executing the search
    /// </summary>
    /// <returns>The search specification</returns>
    public SearchSpecification GetSpecification()
    {
        return _specification;
    }
}
