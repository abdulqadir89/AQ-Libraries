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
