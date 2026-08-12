using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace AQ.Utilities.Search.PostgreSql;

/// <summary>
/// Translates Fuzzy/Phonetic/FullText search conditions into PostgreSQL functions
/// from the pg_trgm, fuzzystrmatch, and built-in full-text search facilities.
/// Register via <see cref="PostgreSqlSearchRegistration.Register"/> or
/// <see cref="PostgreSqlSearchRegistration.AddPostgreSqlSearch"/>.
/// </summary>
public class PostgreSqlSearchDialect : ISearchDialect
{
    private static readonly MethodInfo TrigramsSimilarityMethod =
        typeof(NpgsqlTrigramsDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlTrigramsDbFunctionsExtensions.TrigramsSimilarity))!;

    private static readonly MethodInfo FuzzyStringMatchSoundexMethod =
        typeof(NpgsqlFuzzyStringMatchDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlFuzzyStringMatchDbFunctionsExtensions.FuzzyStringMatchSoundex))!;

    private static readonly MethodInfo ToTsVectorMethod =
        typeof(NpgsqlFullTextSearchDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlFullTextSearchDbFunctionsExtensions.ToTsVector),
            new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

    private static readonly MethodInfo PlainToTsQueryMethod =
        typeof(NpgsqlFullTextSearchDbFunctionsExtensions).GetMethod(
            nameof(NpgsqlFullTextSearchDbFunctionsExtensions.PlainToTsQuery),
            new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

    private static readonly MethodInfo MatchesMethod =
        typeof(NpgsqlFullTextSearchLinqExtensions).GetMethod(
            nameof(NpgsqlFullTextSearchLinqExtensions.Matches),
            new[] { typeof(NpgsqlTsVector), typeof(NpgsqlTsQuery) })!;

    // A property-access expression (not Expression.Constant) so EF's funcletizer does not
    // fold calls made with it as a pure-constant subtree and attempt client-side evaluation
    // of provider stub methods that only have meaning when translated to SQL.
    private static readonly Expression EfFunctions =
        Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

    private const string FullTextConfig = "english";

    public Expression BuildFuzzyPredicate(Expression property, SearchCondition condition)
    {
        if (property.Type != typeof(string))
            return Expression.Constant(false);

        var similarity = Expression.Call(
            TrigramsSimilarityMethod,
            EfFunctions,
            property,
            Expression.Constant(condition.SearchTerm));

        var comparison = Expression.GreaterThanOrEqual(similarity, Expression.Constant(condition.MinSimilarity));
        return GuardStringNull(property, comparison);
    }

    public Expression BuildPhoneticPredicate(Expression property, SearchCondition condition)
    {
        if (property.Type != typeof(string))
            return Expression.Constant(false);

        var propertySoundex = Expression.Call(FuzzyStringMatchSoundexMethod, EfFunctions, property);

        // FuzzyStringMatchSoundex is a server-side-only stub. A call whose subtree never
        // touches the query parameter (e.g. FuzzyStringMatchSoundex(EF.Functions, "term"))
        // gets evaluated client-side by EF's funcletizer before translation, which throws.
        // Compute the term's Soundex client-side instead — Postgres's soundex() and
        // AQ.Utilities.Search's FuzzyMatcher.Soundex both implement the classic American
        // Soundex algorithm, so the codes match.
        var termSoundex = Expression.Constant(FuzzyMatcher.Soundex(condition.SearchTerm));

        var comparison = Expression.Equal(propertySoundex, termSoundex);
        return GuardStringNull(property, comparison);
    }

    public Expression BuildFullTextPredicate(Expression property, SearchCondition condition)
    {
        if (property.Type != typeof(string))
            return Expression.Constant(false);

        var tsVector = Expression.Call(
            ToTsVectorMethod,
            EfFunctions,
            Expression.Constant(FullTextConfig),
            property);

        var tsQuery = Expression.Call(
            PlainToTsQueryMethod,
            EfFunctions,
            Expression.Constant(FullTextConfig),
            Expression.Constant(condition.SearchTerm));

        var comparison = Expression.Call(MatchesMethod, tsVector, tsQuery);
        return GuardStringNull(property, comparison);
    }

    private static Expression GuardStringNull(Expression property, Expression comparison)
    {
        var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
        return Expression.AndAlso(notNull, comparison);
    }
}
