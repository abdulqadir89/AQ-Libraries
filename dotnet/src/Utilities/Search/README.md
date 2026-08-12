# Search System

Provider-agnostic search over `IQueryable<T>`, split into three packages:

- **`AQ.Utilities`** (`AQ.Utilities.Search` namespace) — attribute-driven field extraction, Exact/Contains/StartsWith/EndsWith, group logic, weighted scoring. Zero EF-provider dependency; every predicate here is plain LINQ that any EF Core provider translates.
- **`AQ.Utilities.SqlServer`** (`AQ.Utilities.Search.SqlServer`) — SQL Server implementation of Fuzzy/Phonetic/FullText via `DIFFERENCE`, `SOUNDEX`, `FREETEXT`.
- **`AQ.Utilities.PostgreSql`** (`AQ.Utilities.Search.PostgreSql`) — PostgreSQL implementation of Fuzzy/Phonetic/FullText via `pg_trgm`, `fuzzystrmatch`, `tsvector`.

Reference only the provider package matching your database. `Fuzzy`, `Phonetic`, and `FullText` operators throw `NotSupportedException` unless a provider is registered — there is no silent fallback.

Pagination is **not** part of this library. Search returns a filtered, scored `SearchResults<T>`; page it the same way you page any other result set.

## Operator support matrix

| Operator | Core (no provider) | SQL Server | PostgreSQL |
|---|---|---|---|
| Exact / Contains / StartsWith / EndsWith | ✅ (LINQ `Equal`/`Contains`/`StartsWith`/`EndsWith`) | ✅ | ✅ |
| Fuzzy | ❌ throws | `DIFFERENCE(col, term) >= 3` (or `4` when `MinSimilarity >= 0.8`) | `similarity(col, term) >= MinSimilarity` (pg_trgm) |
| Phonetic | ❌ throws | `SOUNDEX(col) = SOUNDEX(term)` | `soundex(col) = soundex(term)` (fuzzystrmatch; term computed client-side via `FuzzyMatcher.Soundex`) |
| FullText | ❌ throws | `FREETEXT(col, term)` — requires a full-text index | `to_tsvector('english', col) @@ plainto_tsquery('english', term)` |

## Quick start

### 1. Mark properties as searchable

```csharp
public class User : Entity
{
    [Searchable(Weight = 2.0)]
    public string Name { get; set; } = default!;

    [Searchable(Weight = 1.5)]
    public string Email { get; set; } = default!;

    // Not [Searchable] — only included by GetDefaultSearchableFields fallback
    public string Department { get; set; } = default!;
}
```

### 2. Register a provider dialect (only if you use Fuzzy/Phonetic/FullText)

SQL Server, at startup:

```csharp
using AQ.Utilities.Search.SqlServer;

services.AddSqlServerSearch();
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddSqlServerSearchFunctions();
}
```

PostgreSQL, at startup:

```csharp
using AQ.Utilities.Search.PostgreSql;

services.AddPostgreSqlSearch();
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddPostgreSqlSearchExtensions(); // HasPostgresExtension("pg_trgm"), ("fuzzystrmatch")
}
```

**Consumer migration duties** (not handled by these packages, per the project's "no auto migrations" rule — write it into your own `/db` migration scripts):

- **SQL Server**: create a full-text catalog and full-text index on any column searched with `SearchOperator.FullText`.
- **PostgreSQL**: the `HasPostgresExtension` calls above generate `CREATE EXTENSION IF NOT EXISTS pg_trgm/fuzzystrmatch` in your EF migration — the connecting role needs `CREATE` privilege (or a DBA pre-creates the extensions). For fuzzy search performance at scale, add a trigram index: `CREATE INDEX ... USING GIN (Name gin_trgm_ops);`.

### 3. Search

```csharp
// Global search across [Searchable] fields (or default string/primitive fields as fallback)
var results = dbContext.Users.GlobalSearch("john smith").Results;

// Composable — stays IQueryable, use in CQRS query handlers
var query = dbContext.Users.ApplyGlobalSearch(request.SearchTerm);

// Field-specific
var results = dbContext.Users.SearchField("Name", "John", SearchOperator.Contains).Results;

// Fluent builder
var results = dbContext.Users
    .CreateSearch()
    .Contains("Department", "Engineering", weight: 1.5)
    .Fuzzy("Name", "jon", weight: 2.0, minSimilarity: 0.7)  // requires a registered provider dialect
    .MinScore(0.5)
    .MaxResults(20)
    .Build();

// Groups
var results = dbContext.Users
    .CreateSearch()
    .BeginGroup(SearchMatchType.All)
        .Contains("Department", "IT")
        .Contains("Name", "john")
    .EndGroup()
    .BeginGroup(SearchMatchType.Any)
        .StartsWith("Email", "admin")
        .Contains("Name", "manager")
    .EndGroup()
    .MinScore(0.4)
    .Build();
```

## `[Searchable]` attribute options

```csharp
[Searchable(
    Weight = 2.0,
    EnableFuzzyMatch = true,
    EnableExactMatch = true,
    EnablePrefixMatch = true,
    SearchFieldName = "FullName",
    MinSearchLength = 2,
    IgnoreCase = true
)]
public string Name { get; set; }
```

A property with a private setter is still eligible — only a missing setter (get-only/computed) excludes it. `[NotMapped]` always excludes it.

## Search result structure

```csharp
public class SearchResult<T>
{
    public T Entity { get; set; }
    public double Score { get; set; }
    public Dictionary<string, double> FieldScores { get; set; }
    public List<SearchCondition> MatchingConditions { get; set; }
}

public class SearchResults<T>
{
    public List<SearchResult<T>> Results { get; set; }
    public int TotalCount { get; set; }
    public SearchSpecification SearchSpecification { get; set; }
    public long ExecutionTimeMs { get; set; }
    public Dictionary<string, object> Statistics { get; set; }
}
```

## API request integration

```csharp
public class SearchUsersQuery : SearchableRequest
{
    public bool IncludeInactive { get; set; } = false;
}

public async Task<SearchResults<UserDto>> Handle(SearchUsersQuery query)
{
    var usersQuery = _context.Users.AsQueryable();
    if (!query.IncludeInactive)
        usersQuery = usersQuery.Where(u => u.IsActive);

    var searchResults = usersQuery.ApplySearch(query);

    // Page separately — search does not paginate.
    var page = searchResults.Results.Skip(skip).Take(pageSize).ToList();
    ...
}
```

## Fuzzy matching algorithms (in-memory scoring, provider-independent)

`FuzzyMatcher` implements Levenshtein distance, Jaro/Jaro-Winkler similarity, and Soundex — used for in-memory re-ranking of the candidate set a provider's DB-side predicate returns (see `ApplySearch`'s `ScoreEntity` step). This is separate from provider dialects, which decide *which rows reach the database result set* in the first place.

```csharp
var distance = FuzzyMatcher.LevenshteinDistance("john", "jon");     // 1
var similarity = FuzzyMatcher.SimilarityRatio("john", "jon");        // 0.75
var soundex = FuzzyMatcher.Soundex("john");                          // "J500"
var combined = FuzzyMatcher.CombinedFuzzyScore("john", "jon");       // weighted blend
```

## Breaking changes from the pre-split version

- **Pagination removed from search.** `PagedSearchableRequest` and `ApplySearchAndPaging` are gone. Use `SearchableRequest` (has `MaxResults` but no page number/size/sort) and page `SearchResults<T>.Results` yourself.
- **Fuzzy/Phonetic/FullText now throw without a registered provider.** Previously `Fuzzy` silently degraded to a `Contains` prefilter, which meant it could never actually find typos. Reference `AQ.Utilities.SqlServer` or `AQ.Utilities.PostgreSql` and call `AddSqlServerSearch()`/`AddPostgreSqlSearch()` at startup.
- **Removed decorative API that had no implementation**: `SearchRankingAlgorithm`, `SearchMatchType.BestMatch`, `EnableHighlighting`/`Highlights`, `SearchCondition.MaxEditDistance`.
- **Bug fix**: `GetDefaultSearchableFields` (the fallback used when a type has no `[Searchable]` attributes) previously excluded every `string` property, because `string` implements `IEnumerable<char>` and was misclassified as a collection. Fixed — plain string properties are now included by default.
- **Bug fix**: private-setter properties were excluded from default extraction based on name heuristics (`*Text`, `*Label`, `Display*`, `Full*`, `*Combined*`). Removed — only a missing setter excludes a property now.
