using System.Linq.Expressions;
using System.Reflection;

namespace AQ.Utilities.Filter;

/// <summary>
/// Builds LINQ expressions from filter specifications for use with EF Core and IQueryable
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Applies filter specification to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="filterSpec">The filter specification</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterSpecification? filterSpec)
    {
        if (filterSpec?.RootGroup == null || !filterSpec.RootGroup.Conditions.Any() && !filterSpec.RootGroup.Groups.Any())
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var filterExpression = BuildGroupExpression(filterSpec.RootGroup, parameter);

        if (filterExpression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
            return query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// Applies a filter expression string directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="filterExpression">The filter expression string (e.g., "Name,contains,John && Age,gt,25")</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string? filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            return query;

        var filterSpec = FilterExpressionParser.ParseComplexExpression(filterExpression);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies a single filter condition directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="propertyPath">The property path to filter on (supports nested properties like "User.Profile.Name")</param>
    /// <param name="operator">The filter operator</param>
    /// <param name="value">The value to filter with</param>
    /// <param name="caseSensitive">Whether string operations should be case-sensitive</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string propertyPath, FilterOperator @operator, object? value, bool caseSensitive = false)
    {
        var filterSpec = FilterSpecification.Create(propertyPath, @operator, value, caseSensitive);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies a between filter condition directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="propertyPath">The property path to filter on</param>
    /// <param name="minValue">The minimum value (inclusive)</param>
    /// <param name="maxValue">The maximum value (inclusive)</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyBetweenFilter<T>(this IQueryable<T> query, string propertyPath, object? minValue, object? maxValue)
    {
        var condition = new FilterCondition
        {
            PropertyPath = propertyPath,
            Operator = FilterOperator.Between,
            Value = minValue,
            SecondValue = maxValue
        };

        var filterSpec = new FilterSpecification
        {
            RootGroup = new FilterGroup
            {
                Conditions = new List<FilterCondition> { condition }
            }
        };

        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies an In filter condition directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="propertyPath">The property path to filter on</param>
    /// <param name="values">The values to check for inclusion</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyInFilter<T>(this IQueryable<T> query, string propertyPath, IEnumerable<object?> values)
    {
        var condition = new FilterCondition
        {
            PropertyPath = propertyPath,
            Operator = FilterOperator.In,
            Value = values
        };

        var filterSpec = new FilterSpecification
        {
            RootGroup = new FilterGroup
            {
                Conditions = new List<FilterCondition> { condition }
            }
        };

        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies multiple filter conditions with AND logic directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="conditions">The filter conditions to apply</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyAndFilters<T>(this IQueryable<T> query, params FilterCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return query;

        var filterSpec = new FilterSpecification
        {
            RootGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.And,
                Conditions = conditions.ToList()
            }
        };

        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies multiple filter conditions with OR logic directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="conditions">The filter conditions to apply</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyOrFilters<T>(this IQueryable<T> query, params FilterCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return query;

        var filterSpec = new FilterSpecification
        {
            RootGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Conditions = conditions.ToList()
            }
        };

        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies multiple filter expression strings with AND logic directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="filterExpressions">The filter expression strings</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyAndFilters<T>(this IQueryable<T> query, params string[] filterExpressions)
    {
        if (filterExpressions == null || filterExpressions.Length == 0)
            return query;

        var filterSpec = FilterExpressionParser.ParseConditions(filterExpressions, LogicalOperator.And);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies multiple filter expression strings with OR logic directly to an IQueryable
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="filterExpressions">The filter expression strings</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> ApplyOrFilters<T>(this IQueryable<T> query, params string[] filterExpressions)
    {
        if (filterExpressions == null || filterExpressions.Length == 0)
            return query;

        var filterSpec = FilterExpressionParser.ParseConditions(filterExpressions, LogicalOperator.Or);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies an equal filter condition
    /// </summary>
    public static IQueryable<T> WhereEqual<T>(this IQueryable<T> query, string propertyPath, object? value)
        => query.ApplyFilter(propertyPath, FilterOperator.Equal, value);

    /// <summary>
    /// Applies a not equal filter condition
    /// </summary>
    public static IQueryable<T> WhereNotEqual<T>(this IQueryable<T> query, string propertyPath, object? value)
        => query.ApplyFilter(propertyPath, FilterOperator.NotEqual, value);

    /// <summary>
    /// Applies a greater than filter condition
    /// </summary>
    public static IQueryable<T> WhereGreaterThan<T>(this IQueryable<T> query, string propertyPath, object? value)
        => query.ApplyFilter(propertyPath, FilterOperator.GreaterThan, value);

    /// <summary>
    /// Applies a greater than or equal filter condition
    /// </summary>
    public static IQueryable<T> WhereGreaterThanOrEqual<T>(this IQueryable<T> query, string propertyPath, object? value)
        => query.ApplyFilter(propertyPath, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>
    /// Applies a less than filter condition
    /// </summary>
    public static IQueryable<T> WhereLessThan<T>(this IQueryable<T> query, string propertyPath, object? value)
        => query.ApplyFilter(propertyPath, FilterOperator.LessThan, value);

    /// <summary>
    /// Applies a less than or equal filter condition
    /// </summary>
    public static IQueryable<T> WhereLessThanOrEqual<T>(this IQueryable<T> query, string propertyPath, object? value)
        => query.ApplyFilter(propertyPath, FilterOperator.LessThanOrEqual, value);

    /// <summary>
    /// Applies a contains filter condition (case-insensitive by default)
    /// </summary>
    public static IQueryable<T> WhereContains<T>(this IQueryable<T> query, string propertyPath, string? value, bool caseSensitive = false)
        => query.ApplyFilter(propertyPath, FilterOperator.Contains, value, caseSensitive);

    /// <summary>
    /// Applies a not contains filter condition (case-insensitive by default)
    /// </summary>
    public static IQueryable<T> WhereNotContains<T>(this IQueryable<T> query, string propertyPath, string? value, bool caseSensitive = false)
        => query.ApplyFilter(propertyPath, FilterOperator.NotContains, value, caseSensitive);

    /// <summary>
    /// Applies a starts with filter condition (case-insensitive by default)
    /// </summary>
    public static IQueryable<T> WhereStartsWith<T>(this IQueryable<T> query, string propertyPath, string? value, bool caseSensitive = false)
        => query.ApplyFilter(propertyPath, FilterOperator.StartsWith, value, caseSensitive);

    /// <summary>
    /// Applies an ends with filter condition (case-insensitive by default)
    /// </summary>
    public static IQueryable<T> WhereEndsWith<T>(this IQueryable<T> query, string propertyPath, string? value, bool caseSensitive = false)
        => query.ApplyFilter(propertyPath, FilterOperator.EndsWith, value, caseSensitive);

    /// <summary>
    /// Applies an is null filter condition
    /// </summary>
    public static IQueryable<T> WhereIsNull<T>(this IQueryable<T> query, string propertyPath)
        => query.ApplyFilter(propertyPath, FilterOperator.IsNull, null);

    /// <summary>
    /// Applies an is not null filter condition
    /// </summary>
    public static IQueryable<T> WhereIsNotNull<T>(this IQueryable<T> query, string propertyPath)
        => query.ApplyFilter(propertyPath, FilterOperator.IsNotNull, null);

    /// <summary>
    /// Applies a between filter condition (inclusive)
    /// </summary>
    public static IQueryable<T> WhereBetween<T>(this IQueryable<T> query, string propertyPath, object? minValue, object? maxValue)
        => query.ApplyBetweenFilter(propertyPath, minValue, maxValue);

    /// <summary>
    /// Applies an in filter condition
    /// </summary>
    public static IQueryable<T> WhereIn<T>(this IQueryable<T> query, string propertyPath, params object?[] values)
        => query.ApplyInFilter(propertyPath, values);

    /// <summary>
    /// Applies an in filter condition
    /// </summary>
    public static IQueryable<T> WhereIn<T>(this IQueryable<T> query, string propertyPath, IEnumerable<object?> values)
        => query.ApplyInFilter(propertyPath, values);

    /// <summary>
    /// Applies a not in filter condition
    /// </summary>
    public static IQueryable<T> WhereNotIn<T>(this IQueryable<T> query, string propertyPath, params object?[] values)
        => query.ApplyFilter(propertyPath, FilterOperator.NotIn, values);

    /// <summary>
    /// Applies a not in filter condition
    /// </summary>
    public static IQueryable<T> WhereNotIn<T>(this IQueryable<T> query, string propertyPath, IEnumerable<object?> values)
        => query.ApplyFilter(propertyPath, FilterOperator.NotIn, values);

    /// <summary>
    /// Builds a predicate expression from filter specification
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="filterSpec">The filter specification</param>
    /// <returns>Predicate expression</returns>
    public static Expression<Func<T, bool>>? BuildPredicate<T>(FilterSpecification? filterSpec)
    {
        if (filterSpec?.RootGroup == null || !filterSpec.RootGroup.Conditions.Any() && !filterSpec.RootGroup.Groups.Any())
            return null;

        var parameter = Expression.Parameter(typeof(T), "x");
        var filterExpression = BuildGroupExpression(filterSpec.RootGroup, parameter);

        if (filterExpression != null)
        {
            return Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
        }

        return null;
    }

    private static Expression? BuildGroupExpression(FilterGroup group, ParameterExpression parameter)
    {
        var expressions = new List<Expression>();

        // Build expressions for individual conditions
        foreach (var condition in group.Conditions)
        {
            var conditionExpression = BuildConditionExpression(condition, parameter);
            if (conditionExpression != null)
            {
                expressions.Add(conditionExpression);
            }
        }

        // Build expressions for nested groups
        foreach (var nestedGroup in group.Groups)
        {
            var groupExpression = BuildGroupExpression(nestedGroup, parameter);
            if (groupExpression != null)
            {
                expressions.Add(groupExpression);
            }
        }

        if (!expressions.Any())
            return null;

        // Combine expressions with the logical operator
        Expression? result = expressions.First();
        for (int i = 1; i < expressions.Count; i++)
        {
            result = group.LogicalOperator == LogicalOperator.And
                ? Expression.AndAlso(result!, expressions[i])
                : Expression.OrElse(result!, expressions[i]);
        }

        // Apply negation if needed
        if (group.IsNegated && result != null)
        {
            result = Expression.Not(result);
        }

        return result;
    }

    private static Expression? BuildConditionExpression(FilterCondition condition, ParameterExpression parameter)
    {
        try
        {
            var propertyExpression = GetPropertyExpression(parameter, condition.PropertyPath);
            if (propertyExpression == null)
                return null;

            return condition.Operator switch
            {
                FilterOperator.Equal => BuildEqualExpression(propertyExpression, condition.Value),
                FilterOperator.NotEqual => BuildNotEqualExpression(propertyExpression, condition.Value),
                FilterOperator.GreaterThan => BuildGreaterThanExpression(propertyExpression, condition.Value),
                FilterOperator.GreaterThanOrEqual => BuildGreaterThanOrEqualExpression(propertyExpression, condition.Value),
                FilterOperator.LessThan => BuildLessThanExpression(propertyExpression, condition.Value),
                FilterOperator.LessThanOrEqual => BuildLessThanOrEqualExpression(propertyExpression, condition.Value),
                FilterOperator.Contains => BuildContainsExpression(propertyExpression, condition.Value, condition.CaseSensitive),
                FilterOperator.NotContains => BuildNotContainsExpression(propertyExpression, condition.Value, condition.CaseSensitive),
                FilterOperator.StartsWith => BuildStartsWithExpression(propertyExpression, condition.Value, condition.CaseSensitive),
                FilterOperator.EndsWith => BuildEndsWithExpression(propertyExpression, condition.Value, condition.CaseSensitive),
                FilterOperator.IsNull => BuildIsNullExpression(propertyExpression),
                FilterOperator.IsNotNull => BuildIsNotNullExpression(propertyExpression),
                FilterOperator.In => BuildInExpression(propertyExpression, condition.Value),
                FilterOperator.NotIn => BuildNotInExpression(propertyExpression, condition.Value),
                FilterOperator.Between => BuildBetweenExpression(propertyExpression, condition.Value, condition.SecondValue),
                FilterOperator.NotBetween => BuildNotBetweenExpression(propertyExpression, condition.Value, condition.SecondValue),
                _ => null
            };
        }
        catch
        {
            // Return null for invalid expressions
            return null;
        }
    }

    private static Expression? GetPropertyExpression(Expression parameter, string propertyPath)
    {
        var properties = propertyPath.Split('.');
        Expression expression = parameter;

        foreach (var propertyName in properties)
        {
            var propertyInfo = expression.Type.GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                return null;

            expression = Expression.Property(expression, propertyInfo);
        }

        return expression;
    }

    private static Expression? BuildEqualExpression(Expression propertyExpression, object? value)
    {
        var constantExpression = GetConstantExpression(value, propertyExpression.Type);
        if (constantExpression == null)
            return null;

        return Expression.Equal(propertyExpression, constantExpression);
    }

    private static Expression? BuildNotEqualExpression(Expression propertyExpression, object? value)
    {
        var constantExpression = GetConstantExpression(value, propertyExpression.Type);
        if (constantExpression == null)
            return null;

        return Expression.NotEqual(propertyExpression, constantExpression);
    }

    private static Expression? BuildGreaterThanExpression(Expression propertyExpression, object? value)
    {
        var constantExpression = GetConstantExpression(value, propertyExpression.Type);
        if (constantExpression == null)
            return null;

        return Expression.GreaterThan(propertyExpression, constantExpression);
    }

    private static Expression? BuildGreaterThanOrEqualExpression(Expression propertyExpression, object? value)
    {
        var constantExpression = GetConstantExpression(value, propertyExpression.Type);
        if (constantExpression == null)
            return null;

        return Expression.GreaterThanOrEqual(propertyExpression, constantExpression);
    }

    private static Expression? BuildLessThanExpression(Expression propertyExpression, object? value)
    {
        var constantExpression = GetConstantExpression(value, propertyExpression.Type);
        if (constantExpression == null)
            return null;

        return Expression.LessThan(propertyExpression, constantExpression);
    }

    private static Expression? BuildLessThanOrEqualExpression(Expression propertyExpression, object? value)
    {
        var constantExpression = GetConstantExpression(value, propertyExpression.Type);
        if (constantExpression == null)
            return null;

        return Expression.LessThanOrEqual(propertyExpression, constantExpression);
    }

    private static Expression? BuildContainsExpression(Expression propertyExpression, object? value, bool caseSensitive)
    {
        if (value == null || propertyExpression.Type != typeof(string))
            return null;

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return null;

        var stringExpression = propertyExpression;
        var valueExpression = Expression.Constant(stringValue);

        if (!caseSensitive)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            stringExpression = Expression.Call(stringExpression, toLowerMethod!);
            valueExpression = Expression.Constant(stringValue.ToLower());
        }

        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        return Expression.Call(stringExpression, containsMethod!, valueExpression);
    }

    private static Expression? BuildNotContainsExpression(Expression propertyExpression, object? value, bool caseSensitive)
    {
        var containsExpression = BuildContainsExpression(propertyExpression, value, caseSensitive);
        return containsExpression != null ? Expression.Not(containsExpression) : null;
    }

    private static Expression? BuildStartsWithExpression(Expression propertyExpression, object? value, bool caseSensitive)
    {
        if (value == null || propertyExpression.Type != typeof(string))
            return null;

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return null;

        var stringExpression = propertyExpression;
        var valueExpression = Expression.Constant(stringValue);

        if (!caseSensitive)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            stringExpression = Expression.Call(stringExpression, toLowerMethod!);
            valueExpression = Expression.Constant(stringValue.ToLower());
        }

        var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        return Expression.Call(stringExpression, startsWithMethod!, valueExpression);
    }

    private static Expression? BuildEndsWithExpression(Expression propertyExpression, object? value, bool caseSensitive)
    {
        if (value == null || propertyExpression.Type != typeof(string))
            return null;

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return null;

        var stringExpression = propertyExpression;
        var valueExpression = Expression.Constant(stringValue);

        if (!caseSensitive)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            stringExpression = Expression.Call(stringExpression, toLowerMethod!);
            valueExpression = Expression.Constant(stringValue.ToLower());
        }

        var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        return Expression.Call(stringExpression, endsWithMethod!, valueExpression);
    }

    private static Expression BuildIsNullExpression(Expression propertyExpression)
    {
        var nullConstant = Expression.Constant(null, propertyExpression.Type);
        return Expression.Equal(propertyExpression, nullConstant);
    }

    private static Expression BuildIsNotNullExpression(Expression propertyExpression)
    {
        var nullConstant = Expression.Constant(null, propertyExpression.Type);
        return Expression.NotEqual(propertyExpression, nullConstant);
    }

    private static Expression? BuildInExpression(Expression propertyExpression, object? value)
    {
        if (value is not IEnumerable<object> values)
            return null;

        var valuesList = values.ToList();
        if (!valuesList.Any())
            return Expression.Constant(false); // Empty list means no matches

        Expression? result = null;
        foreach (var item in valuesList)
        {
            var equalExpression = BuildEqualExpression(propertyExpression, item);
            if (equalExpression != null)
            {
                result = result == null ? equalExpression : Expression.OrElse(result, equalExpression);
            }
        }

        return result;
    }

    private static Expression? BuildNotInExpression(Expression propertyExpression, object? value)
    {
        var inExpression = BuildInExpression(propertyExpression, value);
        return inExpression != null ? Expression.Not(inExpression) : null;
    }

    private static Expression? BuildBetweenExpression(Expression propertyExpression, object? value1, object? value2)
    {
        var constant1 = GetConstantExpression(value1, propertyExpression.Type);
        var constant2 = GetConstantExpression(value2, propertyExpression.Type);

        if (constant1 == null || constant2 == null)
            return null;

        var greaterThanOrEqual = Expression.GreaterThanOrEqual(propertyExpression, constant1);
        var lessThanOrEqual = Expression.LessThanOrEqual(propertyExpression, constant2);

        return Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
    }

    private static Expression? BuildNotBetweenExpression(Expression propertyExpression, object? value1, object? value2)
    {
        var betweenExpression = BuildBetweenExpression(propertyExpression, value1, value2);
        return betweenExpression != null ? Expression.Not(betweenExpression) : null;
    }

    private static ConstantExpression? GetConstantExpression(object? value, Type targetType)
    {
        if (value == null)
            return Expression.Constant(null, targetType);

        try
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Special handling for enums
            if (underlyingType.IsEnum)
            {
                return HandleEnumConversion(value, targetType, underlyingType);
            }

            // Convert value to target type
            var convertedValue = Convert.ChangeType(value, underlyingType);
            return Expression.Constant(convertedValue, targetType);
        }
        catch
        {
            return null;
        }
    }

    private static ConstantExpression? HandleEnumConversion(object value, Type targetType, Type enumType)
    {
        try
        {
            object? enumValue = null;

            // If the value is already the correct enum type, use it directly
            if (value.GetType() == enumType)
            {
                enumValue = value;
            }
            // If value is a string, try to parse it as enum name or numeric value
            else if (value is string stringValue)
            {
                // First try to parse as enum name (case-insensitive)
                if (Enum.TryParse(enumType, stringValue, ignoreCase: true, out var parsedEnum))
                {
                    enumValue = parsedEnum;
                }
                // If enum name parsing fails, try to parse as numeric value
                else if (int.TryParse(stringValue, out var intValue) && Enum.IsDefined(enumType, intValue))
                {
                    enumValue = Enum.ToObject(enumType, intValue);
                }
                else
                {
                    return null; // Could not parse as enum
                }
            }
            // If value is numeric, try to convert to enum
            else if (IsNumericType(value.GetType()))
            {
                var intValue = Convert.ToInt32(value);
                if (Enum.IsDefined(enumType, intValue))
                {
                    enumValue = Enum.ToObject(enumType, intValue);
                }
                else
                {
                    return null; // Invalid enum value
                }
            }
            else
            {
                return null; // Unsupported value type for enum conversion
            }

            return enumValue != null ? Expression.Constant(enumValue, targetType) : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }
}
