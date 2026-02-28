namespace AQ.Utilities.Search;

/// <summary>
/// Represents a single search condition
/// </summary>
public class SearchCondition
{
    /// <summary>
    /// The property path to search on (supports nested properties like "User.Profile.Name")
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// The search operator to apply
    /// </summary>
    public SearchOperator Operator { get; set; } = SearchOperator.Contains;

    /// <summary>
    /// The search term/value
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Weight/importance of this search condition (higher = more important)
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Maximum edit distance for fuzzy matching
    /// </summary>
    public int MaxEditDistance { get; set; } = 2;

    /// <summary>
    /// Indicates if the search should be case-sensitive
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Minimum similarity threshold (0.0 to 1.0) for fuzzy matching
    /// </summary>
    public double MinSimilarity { get; set; } = 0.6;
}

/// <summary>
/// Represents a group of search conditions with logical operators
/// </summary>
public class SearchGroup
{
    /// <summary>
    /// Individual search conditions in this group
    /// </summary>
    public List<SearchCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Nested search groups
    /// </summary>
    public List<SearchGroup> Groups { get; set; } = new();

    /// <summary>
    /// How to match conditions within this group
    /// </summary>
    public SearchMatchType MatchType { get; set; } = SearchMatchType.Any;

    /// <summary>
    /// Indicates if this group should be negated (NOT)
    /// </summary>
    public bool IsNegated { get; set; } = false;
}

/// <summary>
/// Main search specification containing all search criteria
/// </summary>
public class SearchSpecification
{
    /// <summary>
    /// Root search group containing all search conditions
    /// </summary>
    public SearchGroup RootGroup { get; set; } = new();

    /// <summary>
    /// Global search term that applies to all searchable fields
    /// </summary>
    public string? GlobalSearchTerm { get; set; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int? MaxResults { get; set; }

    /// <summary>
    /// Minimum score threshold for results (0.0 to 1.0)
    /// </summary>
    public double MinScore { get; set; } = 0.1;

    /// <summary>
    /// Ranking algorithm to use for scoring results
    /// </summary>
    public SearchRankingAlgorithm RankingAlgorithm { get; set; } = SearchRankingAlgorithm.WeightBased;

    /// <summary>
    /// Whether to enable fuzzy matching globally
    /// </summary>
    public bool EnableFuzzyMatch { get; set; } = true;

    /// <summary>
    /// Whether to highlight matching terms in results
    /// </summary>
    public bool EnableHighlighting { get; set; } = false;

    /// <summary>
    /// Creates a simple search specification with a single global search term
    /// </summary>
    public static SearchSpecification Create(string searchTerm, SearchOperator op = SearchOperator.Contains, double minScore = 0.1)
    {
        return new SearchSpecification
        {
            GlobalSearchTerm = searchTerm,
            MinScore = minScore,
            RootGroup = new SearchGroup
            {
                MatchType = SearchMatchType.Any
            }
        };
    }

    /// <summary>
    /// Creates a search specification for a specific field
    /// </summary>
    public static SearchSpecification CreateForField(string propertyPath, string searchTerm, SearchOperator op = SearchOperator.Contains, double weight = 1.0)
    {
        return new SearchSpecification
        {
            RootGroup = new SearchGroup
            {
                Conditions = new List<SearchCondition>
                {
                    new SearchCondition
                    {
                        PropertyPath = propertyPath,
                        SearchTerm = searchTerm,
                        Operator = op,
                        Weight = weight
                    }
                }
            }
        };
    }
}

/// <summary>
/// Represents a search result with scoring information
/// </summary>
/// <typeparam name="T">The type of the entity being searched</typeparam>
public class SearchResult<T>
{
    /// <summary>
    /// The entity that matched the search criteria
    /// </summary>
    public T Entity { get; set; } = default!;

    /// <summary>
    /// Overall search score (0.0 to 1.0, higher is better)
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Detailed scoring breakdown by field
    /// </summary>
    public Dictionary<string, double> FieldScores { get; set; } = new();

    /// <summary>
    /// Highlighted search results (if highlighting is enabled)
    /// </summary>
    public Dictionary<string, string> Highlights { get; set; } = new();

    /// <summary>
    /// Matching conditions that contributed to this result
    /// </summary>
    public List<SearchCondition> MatchingConditions { get; set; } = new();
}

/// <summary>
/// Represents a collection of search results with metadata
/// </summary>
/// <typeparam name="T">The type of the entities being searched</typeparam>
public class SearchResults<T>
{
    /// <summary>
    /// The search results ordered by relevance score
    /// </summary>
    public List<SearchResult<T>> Results { get; set; } = new();

    /// <summary>
    /// Total number of matches found (before any limit is applied)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The search specification that was used
    /// </summary>
    public SearchSpecification SearchSpecification { get; set; } = new();

    /// <summary>
    /// Time taken to execute the search (in milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Search statistics and debugging information
    /// </summary>
    public Dictionary<string, object> Statistics { get; set; } = new();
}


