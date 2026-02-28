namespace AQ.Utilities.Search;

/// <summary>
/// Supported search operators for dynamic searching
/// </summary>
public enum SearchOperator
{
    /// <summary>
    /// Exact match
    /// </summary>
    Exact,

    /// <summary>
    /// Contains (partial match)
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with (prefix match)
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with (suffix match)
    /// </summary>
    EndsWith,

    /// <summary>
    /// Fuzzy match using Levenshtein distance
    /// </summary>
    Fuzzy,

    /// <summary>
    /// Phonetic match using Soundex or similar algorithm
    /// </summary>
    Phonetic,

    /// <summary>
    /// Full-text search (if supported by database)
    /// </summary>
    FullText
}

/// <summary>
/// Search match type for combining multiple search criteria
/// </summary>
public enum SearchMatchType
{
    /// <summary>
    /// Any field must match (OR logic)
    /// </summary>
    Any,

    /// <summary>
    /// All fields must match (AND logic)
    /// </summary>
    All,

    /// <summary>
    /// Best match based on weighted scoring
    /// </summary>
    BestMatch
}

/// <summary>
/// Search result ranking algorithm
/// </summary>
public enum SearchRankingAlgorithm
{
    /// <summary>
    /// Simple weight-based scoring
    /// </summary>
    WeightBased,

    /// <summary>
    /// TF-IDF (Term Frequency-Inverse Document Frequency)
    /// </summary>
    TfIdf,

    /// <summary>
    /// BM25 algorithm
    /// </summary>
    Bm25,

    /// <summary>
    /// Custom scoring based on multiple factors
    /// </summary>
    Custom
}
