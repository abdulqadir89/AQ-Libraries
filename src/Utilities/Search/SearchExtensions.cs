using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace AQ.Utilities.Search;

/// <summary>
/// Extension methods for applying search specifications to IQueryable
/// </summary>
public static class SearchExtensions
{
    /// <summary>
    /// Applies a search specification to an IQueryable and returns scored results
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="specification">The search specification</param>
    /// <returns>Search results with scoring information</returns>
    public static SearchResults<T> ApplySearch<T>(this IQueryable<T> query, SearchSpecification? specification) where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        if (specification == null)
        {
            return new SearchResults<T>
            {
                Results = query.Take(100).Select(item => new SearchResult<T> { Entity = item, Score = 1.0 }).ToList(),
                TotalCount = query.Count(),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }

        var searchResults = new SearchResults<T>
        {
            SearchSpecification = specification,
            Statistics = new Dictionary<string, object>()
        };

        try
        {
            // Handle global search term
            if (!string.IsNullOrWhiteSpace(specification.GlobalSearchTerm))
            {
                var globalSearchSpec = SearchableFieldExtractor.CreateGlobalSearchSpecification<T>(specification.GlobalSearchTerm);
                query = ApplySearchConditions(query, globalSearchSpec.RootGroup);
            }

            // Apply specific search conditions
            if (specification.RootGroup.Conditions.Any() || specification.RootGroup.Groups.Any())
            {
                query = ApplySearchConditions(query, specification.RootGroup);
            }

            // Get total count before scoring and limiting
            var totalCount = query.Count();
            searchResults.TotalCount = totalCount;

            // Convert to list for in-memory scoring (this is where we apply fuzzy matching)
            var entities = query.ToList();

            // Apply scoring and fuzzy matching
            var scoredResults = new List<SearchResult<T>>();

            foreach (var entity in entities)
            {
                var searchResult = ScoreEntity(entity, specification);

                if (searchResult.Score >= specification.MinScore)
                {
                    scoredResults.Add(searchResult);
                }
            }

            // Sort by score (descending) and apply limit
            scoredResults = scoredResults
                .OrderByDescending(r => r.Score)
                .ToList();

            if (specification.MaxResults.HasValue)
            {
                scoredResults = scoredResults.Take(specification.MaxResults.Value).ToList();
            }

            searchResults.Results = scoredResults;
            searchResults.Statistics["EntitiesEvaluated"] = entities.Count;
            searchResults.Statistics["ResultsAfterScoring"] = scoredResults.Count;
        }
        catch (Exception ex)
        {
            searchResults.Statistics["Error"] = ex.Message;
        }

        stopwatch.Stop();
        searchResults.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        return searchResults;
    }

    /// <summary>
    /// Applies a simple global search to all searchable fields
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="searchOperator">The search operator to use</param>
    /// <param name="minScore">Minimum score threshold</param>
    /// <returns>Search results with scoring information</returns>
    public static SearchResults<T> GlobalSearch<T>(this IQueryable<T> query, string searchTerm, SearchOperator searchOperator = SearchOperator.Contains, double minScore = 0.1) where T : class
    {
        var specification = SearchSpecification.Create(searchTerm, searchOperator, minScore);
        return query.ApplySearch(specification);
    }

    /// <summary>
    /// Applies a global search to an IQueryable and returns an IQueryable for further composition.
    /// Handles null or empty search terms gracefully by returning the original query unchanged.
    /// This method is designed for use in CQRS queries where you need to continue composing the query.
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="searchTerm">The term to search for (can be null or empty)</param>
    /// <param name="searchOperator">The search operator to use</param>
    /// <returns>An IQueryable that can be further composed with other operations</returns>
    public static IQueryable<T> ApplyGlobalSearch<T>(this IQueryable<T> query, string? searchTerm, SearchOperator searchOperator = SearchOperator.Contains) where T : class
    {
        // Handle null or empty search terms
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        // Create a search specification for the global search
        var globalSearchSpec = SearchableFieldExtractor.CreateGlobalSearchSpecification<T>(
            searchTerm,
            searchOperator,
            includeNestedProperties: true);

        // Apply search conditions and return the filtered IQueryable
        return ApplySearchConditions(query, globalSearchSpec.RootGroup);
    }

    /// <summary>
    /// Applies search conditions from a SearchSpecification to an IQueryable and returns an IQueryable for further composition.
    /// Handles null specifications gracefully by returning the original query unchanged.
    /// This method is designed for use in CQRS queries where you need to continue composing the query.
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="specification">The search specification to apply (can be null)</param>
    /// <returns>An IQueryable that can be further composed with other operations</returns>
    public static IQueryable<T> ApplySearchAsQueryable<T>(this IQueryable<T> query, SearchSpecification? specification) where T : class
    {
        if (specification == null)
        {
            return query;
        }

        // Handle global search term
        if (!string.IsNullOrWhiteSpace(specification.GlobalSearchTerm))
        {
            var globalSearchSpec = SearchableFieldExtractor.CreateGlobalSearchSpecification<T>(specification.GlobalSearchTerm);
            query = ApplySearchConditions(query, globalSearchSpec.RootGroup);
        }

        // Apply specific search conditions
        if (specification.RootGroup.Conditions.Any() || specification.RootGroup.Groups.Any())
        {
            query = ApplySearchConditions(query, specification.RootGroup);
        }

        return query;
    }

    /// <summary>
    /// Searches a specific field
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="propertyPath">The property path to search</param>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="searchOperator">The search operator to use</param>
    /// <param name="weight">Weight for this search condition</param>
    /// <returns>Search results with scoring information</returns>
    public static SearchResults<T> SearchField<T>(this IQueryable<T> query, string propertyPath, string searchTerm, SearchOperator searchOperator = SearchOperator.Contains, double weight = 1.0) where T : class
    {
        var specification = SearchSpecification.CreateForField(propertyPath, searchTerm, searchOperator, weight);
        return query.ApplySearch(specification);
    }

    /// <summary>
    /// Creates a search builder for fluent syntax
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <returns>A search specification builder</returns>
    public static SearchSpecificationBuilder<T> CreateSearch<T>(this IQueryable<T> query) where T : class
    {
        return new SearchSpecificationBuilder<T>(query);
    }

    private static IQueryable<T> ApplySearchConditions<T>(IQueryable<T> query, SearchGroup group) where T : class
    {
        Expression<Func<T, bool>>? combinedExpression = null;

        foreach (var condition in group.Conditions.Where(c => !string.IsNullOrWhiteSpace(c.SearchTerm)))
        {
            var conditionExpression = BuildSearchExpression<T>(condition);

            if (conditionExpression != null)
            {
                if (combinedExpression == null)
                {
                    combinedExpression = conditionExpression;
                }
                else
                {
                    combinedExpression = group.MatchType == SearchMatchType.All
                        ? CombineExpressions(combinedExpression, conditionExpression, ExpressionType.AndAlso)
                        : CombineExpressions(combinedExpression, conditionExpression, ExpressionType.OrElse);
                }
            }
        }

        // Handle nested groups recursively
        foreach (var nestedGroup in group.Groups)
        {
            var nestedQuery = query.Provider.CreateQuery<T>(query.Expression);
            var nestedExpression = BuildGroupExpression<T>(nestedGroup);

            if (nestedExpression != null)
            {
                if (combinedExpression == null)
                {
                    combinedExpression = nestedExpression;
                }
                else
                {
                    combinedExpression = group.MatchType == SearchMatchType.All
                        ? CombineExpressions(combinedExpression, nestedExpression, ExpressionType.AndAlso)
                        : CombineExpressions(combinedExpression, nestedExpression, ExpressionType.OrElse);
                }
            }
        }

        if (combinedExpression != null)
        {
            if (group.IsNegated)
            {
                combinedExpression = Expression.Lambda<Func<T, bool>>(
                    Expression.Not(combinedExpression.Body),
                    combinedExpression.Parameters);
            }

            query = query.Where(combinedExpression);
        }

        return query;
    }

    private static Expression<Func<T, bool>>? BuildGroupExpression<T>(SearchGroup group) where T : class
    {
        Expression<Func<T, bool>>? combinedExpression = null;

        foreach (var condition in group.Conditions.Where(c => !string.IsNullOrWhiteSpace(c.SearchTerm)))
        {
            var conditionExpression = BuildSearchExpression<T>(condition);

            if (conditionExpression != null)
            {
                if (combinedExpression == null)
                {
                    combinedExpression = conditionExpression;
                }
                else
                {
                    combinedExpression = group.MatchType == SearchMatchType.All
                        ? CombineExpressions(combinedExpression, conditionExpression, ExpressionType.AndAlso)
                        : CombineExpressions(combinedExpression, conditionExpression, ExpressionType.OrElse);
                }
            }
        }

        return combinedExpression;
    }

    private static Expression<Func<T, bool>>? BuildSearchExpression<T>(SearchCondition condition) where T : class
    {
        try
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = BuildPropertyExpression(parameter, condition.PropertyPath);

            if (property == null)
                return null;

            // Handle different search operators
            Expression searchExpression = condition.Operator switch
            {
                SearchOperator.Exact => BuildExactExpression(property, condition),
                SearchOperator.Contains => BuildContainsExpression(property, condition),
                SearchOperator.StartsWith => BuildStartsWithExpression(property, condition),
                SearchOperator.EndsWith => BuildEndsWithExpression(property, condition),
                SearchOperator.Fuzzy => BuildFuzzyExpression(property, condition),
                _ => BuildContainsExpression(property, condition)
            };

            return Expression.Lambda<Func<T, bool>>(searchExpression, parameter);
        }
        catch
        {
            return null;
        }
    }

    private static Expression? BuildPropertyExpression(ParameterExpression parameter, string propertyPath)
    {
        Expression property = parameter;
        var properties = propertyPath.Split('.');

        foreach (var prop in properties)
        {
            var propertyInfo = property.Type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null)
                return null;

            property = Expression.Property(property, propertyInfo);
        }

        return property;
    }

    private static Expression BuildExactExpression(Expression property, SearchCondition condition)
    {
        var propertyType = property.Type;
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        if (underlyingType == typeof(string))
        {
            var searchValue = condition.CaseSensitive ? condition.SearchTerm : condition.SearchTerm.ToLowerInvariant();
            var compareProperty = condition.CaseSensitive ? property : Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            return Expression.Equal(compareProperty, Expression.Constant(searchValue));
        }
        else if (underlyingType.IsPrimitive || underlyingType == typeof(Guid) || underlyingType == typeof(decimal))
        {
            // Try to convert search term to the property type
            if (TryConvertSearchTerm(condition.SearchTerm, underlyingType, out var convertedValue))
            {
                var constantValue = Expression.Constant(convertedValue, propertyType);
                return Expression.Equal(property, constantValue);
            }
            else
            {
                // If conversion fails, return false expression
                return Expression.Constant(false);
            }
        }
        
        // Default fallback for unsupported types
        return Expression.Constant(false);
    }

    private static Expression BuildContainsExpression(Expression property, SearchCondition condition)
    {
        var propertyType = property.Type;
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        if (underlyingType == typeof(string))
        {
            var searchValue = condition.CaseSensitive ? condition.SearchTerm : condition.SearchTerm.ToLowerInvariant();
            var compareProperty = condition.CaseSensitive ? property : Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            return Expression.Call(compareProperty, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, Expression.Constant(searchValue));
        }
        else if (underlyingType.IsPrimitive || underlyingType == typeof(Guid) || underlyingType == typeof(decimal))
        {
            // For numeric types, "contains" means exact match for the search term
            if (TryConvertSearchTerm(condition.SearchTerm, underlyingType, out var convertedValue))
            {
                var constantValue = Expression.Constant(convertedValue, propertyType);
                return Expression.Equal(property, constantValue);
            }
            else
            {
                // If conversion fails, return false expression
                return Expression.Constant(false);
            }
        }
        
        // Default fallback for unsupported types
        return Expression.Constant(false);
    }

    private static Expression BuildStartsWithExpression(Expression property, SearchCondition condition)
    {
        var propertyType = property.Type;
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        if (underlyingType == typeof(string))
        {
            var searchValue = condition.CaseSensitive ? condition.SearchTerm : condition.SearchTerm.ToLowerInvariant();
            var compareProperty = condition.CaseSensitive ? property : Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            return Expression.Call(compareProperty, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, Expression.Constant(searchValue));
        }
        else if (underlyingType.IsPrimitive || underlyingType == typeof(Guid) || underlyingType == typeof(decimal))
        {
            // For numeric types, StartsWith behaves like exact match
            return BuildExactExpression(property, condition);
        }
        
        // Default fallback for unsupported types
        return Expression.Constant(false);
    }

    private static Expression BuildEndsWithExpression(Expression property, SearchCondition condition)
    {
        var propertyType = property.Type;
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        if (underlyingType == typeof(string))
        {
            var searchValue = condition.CaseSensitive ? condition.SearchTerm : condition.SearchTerm.ToLowerInvariant();
            var compareProperty = condition.CaseSensitive ? property : Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            return Expression.Call(compareProperty, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, Expression.Constant(searchValue));
        }
        else if (underlyingType.IsPrimitive || underlyingType == typeof(Guid) || underlyingType == typeof(decimal))
        {
            // For numeric types, EndsWith behaves like exact match
            return BuildExactExpression(property, condition);
        }
        
        // Default fallback for unsupported types
        return Expression.Constant(false);
    }

    private static Expression BuildFuzzyExpression(Expression property, SearchCondition condition)
    {
        // For database queries, fuzzy matching will be handled in post-processing
        // Here we apply a basic contains filter to reduce the dataset
        return BuildContainsExpression(property, condition);
    }

    private static Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        ExpressionType type)
    {
        var parameter = left.Parameters[0];
        var rightBody = new ParameterRewriter(right.Parameters[0], parameter).Visit(right.Body);
        var body = type == ExpressionType.AndAlso
            ? Expression.AndAlso(left.Body, rightBody!)
            : Expression.OrElse(left.Body, rightBody!);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static bool TryConvertSearchTerm(string searchTerm, Type targetType, out object? convertedValue)
    {
        convertedValue = null;
        
        try
        {
            if (targetType == typeof(int))
            {
                if (int.TryParse(searchTerm, out var intValue))
                {
                    convertedValue = intValue;
                    return true;
                }
            }
            else if (targetType == typeof(long))
            {
                if (long.TryParse(searchTerm, out var longValue))
                {
                    convertedValue = longValue;
                    return true;
                }
            }
            else if (targetType == typeof(double))
            {
                if (double.TryParse(searchTerm, out var doubleValue))
                {
                    convertedValue = doubleValue;
                    return true;
                }
            }
            else if (targetType == typeof(decimal))
            {
                if (decimal.TryParse(searchTerm, out var decimalValue))
                {
                    convertedValue = decimalValue;
                    return true;
                }
            }
            else if (targetType == typeof(float))
            {
                if (float.TryParse(searchTerm, out var floatValue))
                {
                    convertedValue = floatValue;
                    return true;
                }
            }
            else if (targetType == typeof(Guid))
            {
                if (Guid.TryParse(searchTerm, out var guidValue))
                {
                    convertedValue = guidValue;
                    return true;
                }
            }
            else if (targetType == typeof(DateTime))
            {
                if (DateTime.TryParse(searchTerm, out var dateValue))
                {
                    convertedValue = dateValue;
                    return true;
                }
            }
            else if (targetType == typeof(bool))
            {
                if (bool.TryParse(searchTerm, out var boolValue))
                {
                    convertedValue = boolValue;
                    return true;
                }
            }
            else if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, searchTerm, true, out var enumValue))
                {
                    convertedValue = enumValue;
                    return true;
                }
            }
        }
        catch
        {
            // Ignore conversion errors
        }
        
        return false;
    }

    private static SearchResult<T> ScoreEntity<T>(T entity, SearchSpecification specification) where T : class
    {
        var result = new SearchResult<T>
        {
            Entity = entity,
            Score = 0.0,
            FieldScores = new Dictionary<string, double>(),
            MatchingConditions = new List<SearchCondition>()
        };

        var totalWeight = 0.0;
        var weightedScore = 0.0;

        // Score global search term
        if (!string.IsNullOrWhiteSpace(specification.GlobalSearchTerm))
        {
            var searchableFields = SearchableFieldExtractor.ExtractSearchableFields<T>();
            if (!searchableFields.Any())
            {
                searchableFields = SearchableFieldExtractor.GetDefaultSearchableFields<T>();
            }

            foreach (var field in searchableFields)
            {
                var fieldValue = GetPropertyValue(entity, field.Key);
                if (fieldValue != null)
                {
                    var baseScore = CalculateFieldScore(fieldValue.ToString() ?? string.Empty, specification.GlobalSearchTerm, field.Value);

                    if (baseScore > 0)
                    {
                        result.FieldScores[field.Key] = baseScore;
                        weightedScore += baseScore * field.Value.Weight;
                        totalWeight += field.Value.Weight;
                    }
                }
            }
        }

        // Score specific conditions
        foreach (var condition in GetAllConditions(specification.RootGroup))
        {
            var fieldValue = GetPropertyValue(entity, condition.PropertyPath);
            if (fieldValue != null)
            {
                var fieldInfo = new SearchFieldInfo
                {
                    Weight = condition.Weight,
                    EnableFuzzyMatch = specification.EnableFuzzyMatch,
                    IgnoreCase = !condition.CaseSensitive,
                    MinSearchLength = 1
                };

                var fieldScore = CalculateFieldScore(fieldValue.ToString() ?? string.Empty, condition.SearchTerm, fieldInfo);
                if (fieldScore >= condition.MinSimilarity)
                {
                    result.FieldScores[condition.PropertyPath] = fieldScore;
                    result.MatchingConditions.Add(condition);
                    weightedScore += fieldScore * condition.Weight;
                    totalWeight += condition.Weight;
                }
            }
        }

        result.Score = totalWeight > 0 ? weightedScore / totalWeight : 0.0;
        return result;
    }

    private static double CalculateFieldScore(string fieldValue, string searchTerm, SearchFieldInfo fieldInfo)
    {
        if (string.IsNullOrWhiteSpace(fieldValue) || string.IsNullOrWhiteSpace(searchTerm))
            return 0.0;

        if (searchTerm.Length < fieldInfo.MinSearchLength)
            return 0.0;

        var compareFieldValue = fieldInfo.IgnoreCase ? fieldValue.ToLowerInvariant() : fieldValue;
        var compareSearchTerm = fieldInfo.IgnoreCase ? searchTerm.ToLowerInvariant() : searchTerm;

        double maxScore = 0.0;

        // Exact match gets highest score
        if (fieldInfo.EnableExactMatch && compareFieldValue.Equals(compareSearchTerm))
        {
            maxScore = Math.Max(maxScore, 1.0);
        }

        // Contains match
        if (compareFieldValue.Contains(compareSearchTerm))
        {
            maxScore = Math.Max(maxScore, 0.8);
        }

        // Prefix match
        if (fieldInfo.EnablePrefixMatch && compareFieldValue.StartsWith(compareSearchTerm))
        {
            maxScore = Math.Max(maxScore, 0.9);
        }

        // Fuzzy match
        if (fieldInfo.EnableFuzzyMatch)
        {
            var fuzzyScore = FuzzyMatcher.CombinedFuzzyScore(compareFieldValue, compareSearchTerm);
            maxScore = Math.Max(maxScore, fuzzyScore * 0.7); // Fuzzy matches get lower max score
        }

        return maxScore;
    }

    private static object? GetPropertyValue(object obj, string propertyPath)
    {
        try
        {
            var properties = propertyPath.Split('.');
            object? current = obj;

            foreach (var prop in properties)
            {
                if (current == null)
                    return null;

                var propertyInfo = current.GetType().GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    return null;

                current = propertyInfo.GetValue(current);
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<SearchCondition> GetAllConditions(SearchGroup group)
    {
        foreach (var condition in group.Conditions)
        {
            yield return condition;
        }

        foreach (var nestedGroup in group.Groups)
        {
            foreach (var condition in GetAllConditions(nestedGroup))
            {
                yield return condition;
            }
        }
    }
}

/// <summary>
/// Helper class for rewriting expression parameters
/// </summary>
internal class ParameterRewriter : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterRewriter(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}

/// <summary>
/// Additional search extension methods for common scenarios
/// </summary>
public static class CommonSearchExtensions
{
    /// <summary>
    /// Applies a simple text search across multiple string properties of an entity.
    /// Handles null search terms gracefully by returning the original query unchanged.
    /// 
    /// Example usage:
    /// query.SearchInProperties(searchTerm, 
    ///     u => u.Name, 
    ///     u => u.Abbreviation, 
    ///     u => u.Description)
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="searchTerm">The term to search for (can be null or empty)</param>
    /// <param name="propertySelectors">Functions that select string properties to search</param>
    /// <returns>An IQueryable that can be further composed with other operations</returns>
    public static IQueryable<T> SearchInProperties<T>(this IQueryable<T> query, string? searchTerm, params Expression<Func<T, string?>>[] propertySelectors) where T : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || !propertySelectors.Any())
        {
            return query;
        }

        var lowerSearchTerm = searchTerm.ToLowerInvariant();
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var selector in propertySelectors)
        {
            // Replace the parameter in the selector with our parameter
            var body = new ParameterRewriter(selector.Parameters[0], parameter).Visit(selector.Body);

            // Create null check and contains expression
            var nullCheck = Expression.NotEqual(body, Expression.Constant(null));
            var toLower = Expression.Call(body!, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, Expression.Constant(lowerSearchTerm));
            var condition = Expression.AndAlso(nullCheck, contains);

            combinedExpression = combinedExpression == null
                ? condition
                : Expression.OrElse(combinedExpression, condition);
        }

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// Applies a search that includes navigation properties.
    /// Handles null search terms gracefully by returning the original query unchanged.
    /// 
    /// Example usage for Unit entities with navigation properties:
    /// query.SearchInPropertiesWithNavigation(searchTerm, 
    ///     u => u.Name,
    ///     u => u.Parent.Name,     // Navigation property
    ///     u => u.Manager.Name)    // Navigation property
    /// </summary>
    /// <typeparam name="T">The type of entities being searched</typeparam>
    /// <param name="query">The queryable to search</param>
    /// <param name="searchTerm">The term to search for (can be null or empty)</param>
    /// <param name="propertySelectors">Functions that select string properties to search, including navigation properties</param>
    /// <returns>An IQueryable that can be further composed with other operations</returns>
    public static IQueryable<T> SearchInPropertiesWithNavigation<T>(this IQueryable<T> query, string? searchTerm, params Expression<Func<T, string?>>[] propertySelectors) where T : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || !propertySelectors.Any())
        {
            return query;
        }

        var lowerSearchTerm = searchTerm.ToLowerInvariant();
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var selector in propertySelectors)
        {
            // Replace the parameter in the selector with our parameter
            var body = new ParameterRewriter(selector.Parameters[0], parameter).Visit(selector.Body);

            // Build null checks for navigation properties
            var nullChecks = BuildNavigationNullChecks(body!);
            var toLower = Expression.Call(body!, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, Expression.Constant(lowerSearchTerm));

            Expression condition = contains;
            if (nullChecks != null)
            {
                condition = Expression.AndAlso(nullChecks, condition);
            }

            combinedExpression = combinedExpression == null
                ? condition
                : Expression.OrElse(combinedExpression, condition);
        }

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static Expression? BuildNavigationNullChecks(Expression expression)
    {
        if (expression is MemberExpression memberExpression && memberExpression.Expression != null)
        {
            var parentNullCheck = BuildNavigationNullChecks(memberExpression.Expression);
            var currentNullCheck = Expression.NotEqual(memberExpression.Expression, Expression.Constant(null));

            return parentNullCheck == null
                ? currentNullCheck
                : Expression.AndAlso(parentNullCheck, currentNullCheck);
        }

        return null;
    }

    /// <summary>
    /// Helper class for rewriting expression parameters in CommonSearchExtensions
    /// </summary>
    private class ParameterRewriter : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterRewriter(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
