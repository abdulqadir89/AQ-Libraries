using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace AQ.Utilities.Search.SqlServer;

/// <summary>
/// Translates Fuzzy/Phonetic/FullText search conditions into SQL Server built-in
/// functions (DIFFERENCE, SOUNDEX, FREETEXT). Register via
/// <see cref="SqlServerSearchRegistration.Register"/> or
/// <see cref="SqlServerSearchRegistration.AddSqlServerSearch"/>.
/// </summary>
public class SqlServerSearchDialect : ISearchDialect
{
    private static readonly MethodInfo DifferenceMethod =
        typeof(SqlServerSearchFunctions).GetMethod(nameof(SqlServerSearchFunctions.Difference))!;

    private static readonly MethodInfo SoundexMethod =
        typeof(SqlServerSearchFunctions).GetMethod(nameof(SqlServerSearchFunctions.Soundex))!;

    private static readonly MethodInfo FreeTextMethod =
        typeof(SqlServerDbFunctionsExtensions).GetMethod(
            nameof(SqlServerDbFunctionsExtensions.FreeText),
            new[] { typeof(DbFunctions), typeof(object), typeof(string) })!;

    private static readonly Expression EfFunctions = Expression.Constant(EF.Functions);

    public Expression BuildFuzzyPredicate(Expression property, SearchCondition condition)
    {
        if (property.Type != typeof(string))
            return Expression.Constant(false);

        // DIFFERENCE returns 0-4 (SOUNDEX-code closeness). Map MinSimilarity to a threshold:
        // >= 0.8 requires a close phonetic match (4), otherwise a looser match (3) is accepted.
        var threshold = condition.MinSimilarity >= 0.8 ? 4 : 3;

        var difference = Expression.Call(
            DifferenceMethod,
            property,
            Expression.Constant(condition.SearchTerm));

        var comparison = Expression.GreaterThanOrEqual(difference, Expression.Constant(threshold));
        return GuardStringNull(property, comparison);
    }

    public Expression BuildPhoneticPredicate(Expression property, SearchCondition condition)
    {
        if (property.Type != typeof(string))
            return Expression.Constant(false);

        var propertySoundex = Expression.Call(SoundexMethod, property);
        var termSoundex = Expression.Call(SoundexMethod, Expression.Constant(condition.SearchTerm));

        var comparison = Expression.Equal(propertySoundex, termSoundex);
        return GuardStringNull(property, comparison);
    }

    public Expression BuildFullTextPredicate(Expression property, SearchCondition condition)
    {
        if (property.Type != typeof(string))
            return Expression.Constant(false);

        // Requires a full-text index on the target column (consumer's migration responsibility).
        var comparison = Expression.Call(
            FreeTextMethod,
            EfFunctions,
            Expression.Convert(property, typeof(object)),
            Expression.Constant(condition.SearchTerm));

        return GuardStringNull(property, comparison);
    }

    private static Expression GuardStringNull(Expression property, Expression comparison)
    {
        var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
        return Expression.AndAlso(notNull, comparison);
    }
}
