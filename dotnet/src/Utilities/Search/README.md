# Search System

This library provides a comprehensive fuzzy search and approximate matching system that works with Entity Framework Core, LINQ, and IQueryable. It supports auto-extraction of searchable fields via attributes, multiple search algorithms, and scoring-based result ranking.

## Features

- **Auto-Field Extraction**: Automatically extract searchable fields using `[Searchable]` attribute or default string properties
- **Fuzzy Matching**: Multiple fuzzy matching algorithms including Levenshtein distance, Jaro, Jaro-Winkler, and Soundex
- **Multiple Search Operators**: Exact, Contains, StartsWith, EndsWith, Fuzzy, Phonetic, and FullText search
- **Weighted Scoring**: Assign different weights to fields for relevance-based ranking
- **Nested Property Support**: Search on nested objects (e.g., `User.Profile.Name`)
- **Database Independent**: Works with any EF Core provider
- **Fluent API**: Easy-to-use fluent interface for building complex search queries
- **Request Integration**: Built-in support for API request models
- **Performance Optimized**: Initial database filtering followed by in-memory fuzzy scoring

## Quick Start

### 1. Mark Properties as Searchable

```csharp
public class User : Entity
{
    [Searchable(Weight = 2.0, EnableFuzzyMatch = true)]
    public string Name { get; set; } = default!;
    
    [Searchable(Weight = 1.5)]
    public string Email { get; set; } = default!;
    
    [Searchable(Weight = 1.0, MinSearchLength = 2)]
    public string Department { get; set; } = default!;
    
    // This won't be searchable unless using default extraction
    public DateTime CreatedDate { get; set; }
}
```

### 2. Simple Global Search

```csharp
// Search across all searchable fields
var results = dbContext.Users
    .GlobalSearch("john smith")
    .Results;

// With fuzzy matching
var fuzzyResults = dbContext.Users
    .GlobalSearch("jon smyth", SearchOperator.Fuzzy, minScore: 0.6)
    .Results;
```

### 3. Field-Specific Search

```csharp
// Search in a specific field
var results = dbContext.Users
    .SearchField("Name", "John", SearchOperator.Fuzzy)
    .Results;
```

### 4. Fluent API

```csharp
var results = dbContext.Users
    .CreateSearch()
    .GlobalSearch("john")
    .Contains("Department", "Engineering", weight: 1.5)
    .Fuzzy("Name", "jon", weight: 2.0, minSimilarity: 0.7)
    .MinScore(0.5)
    .MaxResults(20)
    .EnableFuzzyMatch()
    .Build();
```

### 5. Complex Search Groups

```csharp
var results = dbContext.Users
    .CreateSearch()
    .BeginGroup(SearchMatchType.All)
        .Contains("Department", "IT")
        .Fuzzy("Name", "john", minSimilarity: 0.6)
    .EndGroup()
    .BeginGroup(SearchMatchType.Any)
        .StartsWith("Email", "admin")
        .Contains("Name", "manager")
    .EndGroup()
    .MinScore(0.4)
    .Build();
```

## Searchable Attribute Options

```csharp
[Searchable(
    Weight = 2.0,                    // Higher weight = more important in results
    EnableFuzzyMatch = true,         // Enable fuzzy matching for this field
    EnableExactMatch = true,         // Enable exact matching
    EnablePrefixMatch = true,        // Enable prefix matching
    SearchFieldName = "FullName",    // Custom field name for searching
    MinSearchLength = 2,             // Minimum search term length
    IgnoreCase = true               // Case-insensitive search
)]
public string Name { get; set; }
```

## Search Operators

- **Exact**: Exact string matching
- **Contains**: Partial string matching (default)
- **StartsWith**: Prefix matching
- **EndsWith**: Suffix matching
- **Fuzzy**: Fuzzy matching using multiple algorithms
- **Phonetic**: Phonetic matching using Soundex
- **FullText**: Full-text search (database dependent)

## Search Result Structure

```csharp
public class SearchResult<T>
{
    public T Entity { get; set; }                           // The matched entity
    public double Score { get; set; }                       // Overall score (0.0 to 1.0)
    public Dictionary<string, double> FieldScores { get; set; }     // Score per field
    public Dictionary<string, string> Highlights { get; set; }      // Highlighted matches
    public List<SearchCondition> MatchingConditions { get; set; }  // Conditions that matched
}

public class SearchResults<T>
{
    public List<SearchResult<T>> Results { get; set; }      // Ordered by relevance
    public int TotalCount { get; set; }                     // Total matches found
    public SearchSpecification SearchSpecification { get; set; }
    public long ExecutionTimeMs { get; set; }               // Execution time
    public Dictionary<string, object> Statistics { get; set; }     // Debug info
}
```

## API Request Integration

```csharp
public class SearchUsersQuery : PagedSearchableRequest
{
    // Inherits: SearchTerm, SearchOperator, MinScore, EnableFuzzyMatch, etc.
    public bool IncludeInactive { get; set; } = false;
}

// In your handler
public async Task<SearchResults<UserDto>> Handle(SearchUsersQuery query)
{
    var usersQuery = _context.Users.AsQueryable();
    
    if (!query.IncludeInactive)
        usersQuery = usersQuery.Where(u => u.IsActive);
    
    var searchResults = usersQuery.ApplySearchAndPaging(query);
    
    // Map to DTOs
    var dtos = searchResults.Results.Select(r => new SearchResult<UserDto>
    {
        Entity = MapToDto(r.Entity),
        Score = r.Score,
        FieldScores = r.FieldScores
    }).ToList();
    
    return new SearchResults<UserDto>
    {
        Results = dtos,
        TotalCount = searchResults.TotalCount,
        ExecutionTimeMs = searchResults.ExecutionTimeMs
    };
}
```

## Auto-Field Extraction

### With Attributes
```csharp
// Only properties with [Searchable] will be included
var searchableFields = SearchableFieldExtractor.ExtractSearchableFields<User>();
```

### Default Extraction
```csharp
// All string and primitive properties will be included with default settings
var defaultFields = SearchableFieldExtractor.GetDefaultSearchableFields<User>();
```

### Global Search Specification
```csharp
// Creates a search specification that targets all searchable fields
var spec = SearchableFieldExtractor.CreateGlobalSearchSpecification<User>(
    "search term", 
    SearchOperator.Contains,
    includeNestedProperties: true
);
```

## Fuzzy Matching Algorithms

### Available Algorithms
- **Levenshtein Distance**: Edit distance between strings
- **Jaro Similarity**: Character-based similarity
- **Jaro-Winkler**: Enhanced Jaro with prefix bonus
- **Soundex**: Phonetic algorithm for similar sounding words

### Example Usage
```csharp
// Individual algorithms
var distance = FuzzyMatcher.LevenshteinDistance("john", "jon");        // Returns: 1
var similarity = FuzzyMatcher.SimilarityRatio("john", "jon");           // Returns: 0.75
var jaro = FuzzyMatcher.JaroSimilarity("john", "jon");                  // Returns: 0.83
var jaroWinkler = FuzzyMatcher.JaroWinklerSimilarity("john", "jon");    // Returns: 0.9
var soundex = FuzzyMatcher.Soundex("john");                             // Returns: "J500"

// Combined scoring
var combinedScore = FuzzyMatcher.CombinedFuzzyScore("john", "jon");     // Returns: 0.78
```

## Performance Considerations

1. **Database Filtering First**: The system applies basic filters to the database first, then performs fuzzy matching in memory
2. **Use Weights Wisely**: Higher weights on more important fields improve relevance
3. **Set Appropriate MinScore**: Higher minimum scores reduce processing time
4. **Limit MaxResults**: Prevent excessive in-memory processing
5. **Index Searchable Fields**: Ensure database indexes on frequently searched fields

## Advanced Examples

### Nested Property Search
```csharp
public class Employee : Entity
{
    [Searchable(Weight = 2.0)]
    public string Name { get; set; } = default!;
    
    public Department Department { get; set; } = default!;
    public Profile Profile { get; set; } = default!;
}

public class Department : Entity
{
    [Searchable(Weight = 1.5)]
    public string Name { get; set; } = default!;
}

// Search nested properties
var results = dbContext.Employees
    .CreateSearch()
    .Contains("Department.Name", "Engineering")
    .Fuzzy("Profile.Skills", "javascript")
    .Build();
```

### Custom Scoring
```csharp
var results = dbContext.Products
    .CreateSearch()
    .GlobalSearch("laptop")
    .Contains("Category", "Electronics", weight: 1.5)
    .Fuzzy("Brand", "appl", weight: 2.0, minSimilarity: 0.6)
    .StartsWith("Model", "Mac", weight: 1.8)
    .RankingAlgorithm(SearchRankingAlgorithm.Custom)
    .MinScore(0.4)
    .Build();
```

### Multiple Search Groups
```csharp
var results = dbContext.Users
    .CreateSearch()
    .GlobalSearch("admin")
    .BeginGroup(SearchMatchType.Any)
        .Contains("Role", "Administrator")
        .Contains("Role", "Manager")
    .EndGroup()
    .BeginGroup(SearchMatchType.All)
        .StartsWith("Email", "admin")
        .Contains("Department", "IT")
    .EndGroup()
    .Build();
```

## Error Handling

The search system gracefully handles various error scenarios:

- **Invalid Property Names**: Skips invalid property paths
- **Type Conversion Errors**: Continues with other fields
- **Null Values**: Properly handles null comparisons
- **Empty Search Terms**: Returns appropriate default results

## Integration with Existing Systems

The search system is designed to work alongside existing filtering and sorting systems:

```csharp
var results = dbContext.Users
    .ApplyFilter("IsActive,eq,true")           // Apply filters first
    .GlobalSearch("john engineer")              // Then search
    .ApplySort("Department.Name,asc")          // Finally sort
    .Results;
```
