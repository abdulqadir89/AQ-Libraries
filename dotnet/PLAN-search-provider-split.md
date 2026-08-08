# Plan: Split AQ.Utilities.Search into provider-agnostic core + SQL Server / PostgreSQL provider projects

Execute from `d:\Repositories\AQ-Libraries\dotnet`. All commands run from that directory.
Follow existing conventions: central package versions in `Directory.Packages.props` (never `Version=` in csproj), `net10.0`, implicit usings + nullable enabled, `dotnet format AQ.sln` before finishing.

## Context

`src/Utilities/Search/` today mixes three things:
1. A provider-agnostic expression pipeline (`ApplyGlobalSearch`, `ApplySearchAsQueryable`, `SearchInProperties`) — EF-translatable, works, used by DQM.
2. A broken fuzzy path: `SearchOperator.Fuzzy` silently degrades to a SQL `Contains` prefilter, so typo'd values never reach the in-memory fuzzy scorer. Fuzzy must become provider-backed (SQL Server `DIFFERENCE`, PostgreSQL `pg_trgm`) and **throw** when no provider is registered.
3. Pagination/sorting bolted onto search (`PagedSearchableRequest`, `ApplySearchAndPaging`) — being removed; pagination belongs to result handling, not search.

Verified: DQM (the known consumer) uses only `ApplyGlobalSearch` and `[Searchable]` attributes. It does NOT use `PagedSearchableRequest`, `ApplySearchAndPaging`, or `SearchResults<T>` — removing them breaks nothing there.

## Step 1 — Relocate test project one level up

Current: `tests/Utilities/AQ.Utilities.Tests/AQ.Utilities.Tests.csproj`. Target: project file directly at `tests/Utilities/AQ.Utilities.Tests.csproj`.

1. Move all contents of `tests/Utilities/AQ.Utilities.Tests/` (csproj, `Attachments/`, `References/`) up into `tests/Utilities/`. Delete the now-empty `AQ.Utilities.Tests` folder (and its `bin`/`obj`).
2. In the moved csproj, fix the project reference: `..\..\..\src\Utilities\AQ.Utilities.csproj` → `..\..\src\Utilities\AQ.Utilities.csproj`.
3. In `AQ.sln`, update the `AQ.Utilities.Tests` project path to `tests\Utilities\AQ.Utilities.Tests.csproj`.
4. `dotnet build AQ.sln` must pass before continuing.

## Step 2 — Core cleanup in `src/Utilities/Search/` (project AQ.Utilities)

### 2a. Remove pagination from search

- `SearchableRequest.cs`:
  - Delete class `PagedSearchableRequest` entirely.
  - Delete `ApplySearchAndPaging` and the private `GetPropertyValue` helper in `SearchableRequestExtensions`.
  - Add `int? MaxResults { get; set; }` to `ISearchableRequest`.
  - Add a plain `public class SearchableRequest : ISearchableRequest` with the interface members and the same defaults `PagedSearchableRequest` had (`SearchOperator.Contains`, `MinScore = 0.1`, `EnableFuzzyMatch = true`).
  - In `SearchableRequestExtensions.ApplySearch`, replace the `request is PagedSearchableRequest pagedRequest` block with a direct read of `request.MaxResults`.

### 2b. Remove dead/decorative API

- `SearchEnums.cs`: delete enum `SearchRankingAlgorithm`; delete `SearchMatchType.BestMatch` member (keep `Any`, `All`). Keep `SearchOperator.Phonetic` and `SearchOperator.FullText` — they become provider-backed in Step 3.
- `SearchModels.cs`: on `SearchSpecification` delete `RankingAlgorithm` and `EnableHighlighting`; on `SearchResult<T>` delete `Highlights`; on `SearchCondition` delete `MaxEditDistance` (never read anywhere).
- `SearchSpecificationBuilder.cs`: delete `BestMatch()`, `RankingAlgorithm(...)`, `EnableHighlighting(...)`; remove the `maxEditDistance` parameter from `Fuzzy(...)`.

### 2c. Bug fixes

- `SearchableFieldExtractor.IsCollectionType`: add `if (type == typeof(string)) return false;` as the first line. (Bug: `string` implements `IEnumerable<char>`, so today every plain string property is classified as a collection and excluded from default searchable fields.)
- `SearchableFieldExtractor.IsComputedProperty`: reduce the method body to `return !property.CanWrite;`. Delete the name-pattern heuristics (`Contains("Full")`, `EndsWith("Text")`, etc.) — they silently exclude real mapped columns. A private setter is fine (DDD entities use them).
- `SearchExtensions` — string branches of `BuildExactExpression`, `BuildContainsExpression`, `BuildStartsWithExpression`, `BuildEndsWithExpression`: wrap each string comparison in a null guard so the expression is `x.Prop != null && <comparison>`:
  ```csharp
  var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
  return Expression.AndAlso(notNull, comparison);
  ```
  (SQL semantics unchanged — NULL already didn't match; this stops `NullReferenceException` when the same tree runs in memory, e.g. tests or EF InMemory.)
- `SearchExtensions.ApplySearch`: delete the `try { ... } catch (Exception ex) { searchResults.Statistics["Error"] = ex.Message; }` wrapper — keep the body, let exceptions propagate. Silent empty results are worse than an exception.
- `SearchExtensions.BuildSearchExpression`: delete its `try/catch` too; keep the `property == null → return null` skip for unknown property paths (tolerant by design).

## Step 3 — Provider dialect abstraction (core)

New file `src/Utilities/Search/ISearchDialect.cs`:

```csharp
using System.Linq.Expressions;

namespace AQ.Utilities.Search;

/// <summary>
/// Builds provider-specific predicates for search operators that cannot be
/// expressed in provider-agnostic LINQ (fuzzy, phonetic, full-text).
/// The returned expression must be built on top of the supplied property
/// expression (already rooted at the query parameter) and be translatable
/// by the target EF Core provider.
/// </summary>
public interface ISearchDialect
{
    Expression BuildFuzzyPredicate(Expression property, SearchCondition condition);
    Expression BuildPhoneticPredicate(Expression property, SearchCondition condition);
    Expression BuildFullTextPredicate(Expression property, SearchCondition condition);
}

/// <summary>
/// Ambient dialect registration. Set once at startup by a provider package
/// (AQ.Utilities.SqlServer / AQ.Utilities.PostgreSql).
/// </summary>
public static class SearchDialect
{
    public static ISearchDialect? Current { get; set; }
}
```

In `SearchExtensions.BuildSearchExpression`, replace the operator switch:

```csharp
Expression searchExpression = condition.Operator switch
{
    SearchOperator.Exact => BuildExactExpression(property, condition),
    SearchOperator.Contains => BuildContainsExpression(property, condition),
    SearchOperator.StartsWith => BuildStartsWithExpression(property, condition),
    SearchOperator.EndsWith => BuildEndsWithExpression(property, condition),
    SearchOperator.Fuzzy => RequireDialect(condition).BuildFuzzyPredicate(property, condition),
    SearchOperator.Phonetic => RequireDialect(condition).BuildPhoneticPredicate(property, condition),
    SearchOperator.FullText => RequireDialect(condition).BuildFullTextPredicate(property, condition),
    _ => BuildContainsExpression(property, condition)
};
```

with a private helper:

```csharp
private static ISearchDialect RequireDialect(SearchCondition condition) =>
    SearchDialect.Current ?? throw new NotSupportedException(
        $"SearchOperator.{condition.Operator} requires a provider dialect. " +
        "Reference AQ.Utilities.SqlServer or AQ.Utilities.PostgreSql and register it at startup.");
```

Delete `BuildFuzzyExpression` (the silent Contains fallback). `FuzzyMatcher` and the in-memory scoring in `ApplySearch`/`ScoreEntity` stay in core unchanged — they are provider-independent.

## Step 4 — New project `AQ.Utilities.SqlServer`

Location: `src/Utilities/SqlServer/AQ.Utilities.SqlServer.csproj`. Copy the csproj shape of `AQ.Utilities.csproj` (no TFM/version overrides beyond what it has). References:

```xml
<ItemGroup>
  <ProjectReference Include="..\AQ.Utilities.csproj" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
</ItemGroup>
```

Add to `Directory.Packages.props`: `<PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.10" />` (match existing EF Core 10.0.10 entries).

Files (namespace `AQ.Utilities.Search.SqlServer`):

**`SqlServerSearchFunctions.cs`** — CLR stubs mapped to built-in T-SQL functions:

```csharp
public static class SqlServerSearchFunctions
{
    public static int Difference(string a, string b)
        => throw new NotSupportedException("Server-side only. Map with AddSqlServerSearchFunctions().");
    public static string Soundex(string value)
        => throw new NotSupportedException("Server-side only. Map with AddSqlServerSearchFunctions().");
}
```

**`SqlServerSearchModelBuilderExtensions.cs`**:

```csharp
public static ModelBuilder AddSqlServerSearchFunctions(this ModelBuilder modelBuilder)
{
    modelBuilder.HasDbFunction(() => SqlServerSearchFunctions.Difference(default!, default!))
        .HasName("DIFFERENCE").IsBuiltIn();
    modelBuilder.HasDbFunction(() => SqlServerSearchFunctions.Soundex(default!))
        .HasName("SOUNDEX").IsBuiltIn();
    return modelBuilder;
}
```

**`SqlServerSearchDialect.cs`** — implements `ISearchDialect`, all via `Expression.Call` on the methods above (get `MethodInfo` once via `typeof(SqlServerSearchFunctions).GetMethod(...)`, cache in static fields):

- `BuildFuzzyPredicate`: `DIFFERENCE(property, term) >= threshold` where threshold maps from `condition.MinSimilarity`: `>= 0.8 → 4`, otherwise `3`. Non-string property types: return `Expression.Constant(false)`.
- `BuildPhoneticPredicate`: `SOUNDEX(property) == SOUNDEX(term)` — call the mapped Soundex on both the property and `Expression.Constant(condition.SearchTerm)` so both evaluate server-side.
- `BuildFullTextPredicate`: call `SqlServerDbFunctionsExtensions.FreeText(EF.Functions, property, term)` via `Expression.Call` with `Expression.Constant(EF.Functions)` as first argument. (Requires a full-text index on the column — consumer responsibility, document it.)

**`SqlServerSearchRegistration.cs`**:

```csharp
public static class SqlServerSearchRegistration
{
    public static void Register() => SearchDialect.Current = new SqlServerSearchDialect();

    public static IServiceCollection AddSqlServerSearch(this IServiceCollection services)
    {
        Register();
        return services;
    }
}
```

(`Microsoft.Extensions.DependencyInjection` already flows from the AQ.Utilities project reference.)

## Step 5 — New project `AQ.Utilities.PostgreSql`

Location: `src/Utilities/PostgreSql/AQ.Utilities.PostgreSql.csproj`. Same shape; references `AQ.Utilities` + `<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />`.

Add to `Directory.Packages.props`: `<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />` — use the latest 10.x available on nuget.org at implementation time (must match EF Core 10).

Files (namespace `AQ.Utilities.Search.PostgreSql`):

**`PostgreSqlSearchModelBuilderExtensions.cs`**:

```csharp
public static ModelBuilder AddPostgreSqlSearchExtensions(this ModelBuilder modelBuilder)
{
    modelBuilder.HasPostgresExtension("pg_trgm");
    modelBuilder.HasPostgresExtension("fuzzystrmatch");
    return modelBuilder;
}
```

**`PostgreSqlSearchDialect.cs`** — implements `ISearchDialect` via `Expression.Call` on Npgsql's `EF.Functions` extensions (first arg `Expression.Constant(EF.Functions)`; cache `MethodInfo` in static fields; non-string properties → `Expression.Constant(false)`):

- `BuildFuzzyPredicate`: `NpgsqlTrigramsDbFunctionsExtensions.TrigramsSimilarity(EF.Functions, property, term) >= condition.MinSimilarity` (`TrigramsSimilarity` returns `double`; compare with `Expression.GreaterThanOrEqual` against `Expression.Constant(condition.MinSimilarity)`). Translates to `similarity(col, @term) >= @min`.
- `BuildPhoneticPredicate`: `NpgsqlFuzzyStringMatchDbFunctionsExtensions.FuzzyStringMatchSoundex(EF.Functions, property) == FuzzyStringMatchSoundex(EF.Functions, constant(term))`.
- `BuildFullTextPredicate`: `NpgsqlFullTextSearchDbFunctionsExtensions.ToTsVector(EF.Functions, "english", property).Matches(NpgsqlFullTextSearchDbFunctionsExtensions.PlainToTsQuery(EF.Functions, "english", term))` — build the `ToTsVector` call, then call `.Matches(...)` (`NpgsqlTsVector.Matches(NpgsqlTsQuery)`) on it. If exact overload names differ in the installed Npgsql version, resolve by inspecting `NpgsqlFullTextSearchDbFunctionsExtensions` and use the `(DbFunctions, string config, string)` overloads.

**`PostgreSqlSearchRegistration.cs`**: mirror of Step 4 (`Register()` + `AddPostgreSqlSearch()`).

## Step 6 — Solution wiring

- Add both new projects to `AQ.sln` under the existing `Utilities` solution folder (GUID `{AB08B9A8-...}` src side): `dotnet sln AQ.sln add src/Utilities/SqlServer/AQ.Utilities.SqlServer.csproj src/Utilities/PostgreSql/AQ.Utilities.PostgreSql.csproj` then move them into the solution folder (edit .sln NestedProjects section to match how AQ.Utilities is nested).
- `dotnet build AQ.sln` must pass.

## Step 7 — Tests (all in the single `AQ.Utilities.Tests` project at `tests/Utilities/`)

Add to the test csproj: project references to both provider projects, and package references `Microsoft.EntityFrameworkCore` (already versioned centrally). Create folder `Search/` with these test classes. Use xUnit + FluentAssertions (NSubstitute available if needed). Provider translation tests use `query.ToQueryString()` — it generates SQL without opening a connection, so no database/docker is needed.

**Important:** `SearchDialect.Current` is global mutable state. Every test class that sets it must belong to `[Collection("SearchDialect")]` (define one `[CollectionDefinition("SearchDialect")]`) so those tests never run in parallel, and must reset `SearchDialect.Current = null` in `Dispose()`.

1. **`FuzzyMatcherTests`** — pure math, known values:
   - `LevenshteinDistance("kitten","sitting") == 3`; empty-string edges.
   - `SimilarityRatio` identical → 1.0, disjoint → low; `IsSimilar` threshold behavior.
   - `JaroWinklerSimilarity("martha","marhta")` ≈ 0.961 (tolerance 0.01).
   - `Soundex("Robert") == "R163"`, `Soundex("John") == Soundex("Jhon")`.
   - `CombinedFuzzyScore` in [0,1]; throws on wrong-length weights array.

2. **`SearchableFieldExtractorTests`** — define local fixture types:
   - Plain POCO with string + int + `List<string>` props: `GetDefaultSearchableFields` **includes** the string props (regression for the string-as-IEnumerable bug), includes int, excludes the collection.
   - Get-only property excluded; private-setter property **included** (regression for removed name heuristics — e.g. a `DisplayName` with private setter must be included).
   - `[NotMapped]` excluded.
   - `[Searchable(Weight = 5)]` extraction returns the field with weight 5; `SubPaths` registers `Prop.Value` leaf; nested `[Searchable]` recursion respects `maxDepth`.

3. **`SearchExpressionTests`** — in-memory `List<T>.AsQueryable()` through `ApplySearchAsQueryable`/`ApplyGlobalSearch`/`SearchInProperties` and the builder (`GetSpecification()` + `ApplySearchAsQueryable`):
   - Contains/StartsWith/EndsWith/Exact match and non-match; case-insensitive by default; `caseSensitive: true` respected.
   - Entities with `null` string values do not throw and do not match (regression for null guard).
   - Exact on int and Guid; unparseable numeric term matches nothing.
   - Unknown property path: condition skipped, other conditions still applied.
   - Group logic: `All` = AND, `Any` = OR, `Not()` negates; nested `BeginGroup`/`EndGroup`.
   - Null/whitespace search term returns query unchanged.

4. **`SearchDialectGuardTests`** (`[Collection("SearchDialect")]`) — with `SearchDialect.Current = null`, applying a spec containing a `Fuzzy` (and `Phonetic`, `FullText`) condition throws `NotSupportedException` mentioning the operator name.

5. **`SqlServerDialectTests`** (`[Collection("SearchDialect")]`) — define a small `TestDbContext` (one entity, `OnModelCreating` calls `AddSqlServerSearchFunctions()`) configured with `optionsBuilder.UseSqlServer("Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True")`. Register the dialect, build a fuzzy/phonetic query via the search API, assert `ToQueryString()` contains `DIFFERENCE(` / `SOUNDEX(` respectively, and that Contains still produces `LIKE`.

6. **`PostgreSqlDialectTests`** (`[Collection("SearchDialect")]`) — same pattern with `UseNpgsql("Host=localhost;Database=x;Username=x;Password=x")` and `AddPostgreSqlSearchExtensions()`. Assert `ToQueryString()` contains `similarity(` for fuzzy, `soundex(` for phonetic, `to_tsvector` for full-text.

7. **`SearchableRequestTests`** — `ApplySearch(ISearchableRequest)`: null request / empty term returns unscored results; `MaxResults` caps `Results.Count`; `MinScore` filters. Use in-memory queryables. Also assert by compilation that `PagedSearchableRequest` and `ApplySearchAndPaging` no longer exist (i.e., just don't reference them; their removal is verified by the build).

## Step 8 — Docs

- Rewrite `src/Utilities/Search/README.md` (and delete or fold in `SUMMARY.md`): architecture (core + two provider packages), operator support matrix per provider, startup registration snippets, `ModelBuilder` setup, and the consumer's migration duties (SQL Server: full-text catalog/index for `FullText`; PostgreSQL: `CREATE EXTENSION pg_trgm; CREATE EXTENSION fuzzystrmatch;` plus a GIN `gin_trgm_ops` index for fuzzy performance). Note breaking changes: pagination removed from search; fuzzy/phonetic/full-text now throw without a registered provider.
- Update `dotnet/CLAUDE.md` solution layout: add the two new projects under `Utilities/` and correct the tests path to `tests/Utilities/` (project directly there).

## Step 9 — Verify

```bash
dotnet format AQ.sln
dotnet build AQ.sln
dotnet test AQ.sln
```

All three must pass. Do not commit unless asked.

## Out of scope

- No changes to DQM or other consuming repositories.
- No Testcontainers / live database integration tests (translation is verified via `ToQueryString`).
- `Filter/`, `Sort/`, `Results/`, `Attachments/`, `References/` utilities untouched (except the test project move which carries existing test folders along).
