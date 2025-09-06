using System.Linq.Expressions;
using System.Reflection;

namespace AQ.Utilities.Sort;

/// <summary>
/// Extension methods for applying sort specifications to IQueryable
/// </summary>
public static class SortExtensions
{
    /// <summary>
    /// Applies a sort specification to an IQueryable
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, SortSpecification? specification)
    {
        if (specification?.Conditions == null || !specification.Conditions.Any())
            return query;

        IOrderedQueryable<T>? orderedQuery = null;
        var isFirst = true;

        foreach (var condition in specification.GetOrderedConditions())
        {
            if (string.IsNullOrWhiteSpace(condition.PropertyPath))
                continue;

            try
            {
                var lambda = CreateSortExpression<T>(condition.PropertyPath, condition.CaseSensitive);

                if (isFirst)
                {
                    orderedQuery = condition.Direction == SortDirection.Ascending
                        ? query.OrderBy(lambda)
                        : query.OrderByDescending(lambda);
                    isFirst = false;
                }
                else
                {
                    orderedQuery = condition.Direction == SortDirection.Ascending
                        ? orderedQuery!.ThenBy(lambda)
                        : orderedQuery!.ThenByDescending(lambda);
                }
            }
            catch (ArgumentException)
            {
                // Skip invalid property paths
                continue;
            }
        }

        return orderedQuery ?? query;
    }

    /// <summary>
    /// Applies a sort expression string to an IQueryable
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortExpression)
    {
        if (string.IsNullOrWhiteSpace(sortExpression))
            return query;

        try
        {
            var specification = SortExpressionParser.Parse(sortExpression);
            return query.ApplySort(specification);
        }
        catch
        {
            // Return original query if sort expression is invalid
            return query;
        }
    }

    /// <summary>
    /// Applies sorting with fallback to default sort if no sort specification is provided
    /// </summary>
    public static IQueryable<T> ApplySortWithDefault<T>(this IQueryable<T> query, SortSpecification? specification, string defaultSortProperty, SortDirection defaultDirection = SortDirection.Ascending)
    {
        if (specification?.Conditions == null || !specification.Conditions.Any())
        {
            // Apply default sort
            var defaultSpec = SortSpecification.Create(defaultSortProperty, defaultDirection);
            return query.ApplySort(defaultSpec);
        }

        return query.ApplySort(specification);
    }

    /// <summary>
    /// Creates a lambda expression for sorting by property path
    /// </summary>
    private static Expression<Func<T, object?>> CreateSortExpression<T>(string propertyPath, bool caseSensitive = false)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression property = parameter;

        var properties = propertyPath.Split('.');
        Type currentType = typeof(T);

        foreach (var prop in properties)
        {
            var propertyInfo = currentType.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{prop}' not found on type '{currentType.Name}'");
            }

            property = Expression.Property(property, propertyInfo);
            currentType = propertyInfo.PropertyType;
        }

        // Handle string case sensitivity
        if (!caseSensitive && currentType == typeof(string))
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
            property = Expression.Call(property, toLowerMethod);
        }

        // Convert to object for uniform handling
        if (property.Type.IsValueType)
        {
            property = Expression.Convert(property, typeof(object));
        }

        return Expression.Lambda<Func<T, object?>>(property, parameter);
    }

    /// <summary>
    /// Validates if all property paths in a sort specification exist on the type
    /// </summary>
    public static bool IsValidForType<T>(this SortSpecification specification)
    {
        if (specification?.Conditions == null)
            return true;

        foreach (var condition in specification.Conditions)
        {
            if (!IsValidPropertyPath<T>(condition.PropertyPath))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates if a property path exists on the type
    /// </summary>
    public static bool IsValidPropertyPath<T>(string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
            return false;

        try
        {
            var properties = propertyPath.Split('.');
            Type currentType = typeof(T);

            foreach (var prop in properties)
            {
                var propertyInfo = currentType.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                    return false;

                currentType = propertyInfo.PropertyType;

                // Handle nullable types
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    currentType = currentType.GetGenericArguments()[0];
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets available sortable properties for a type
    /// </summary>
    public static string[] GetSortableProperties<T>(bool includeNestedProperties = false, int maxDepth = 2)
    {
        return GetSortableProperties(typeof(T), includeNestedProperties, maxDepth);
    }

    /// <summary>
    /// Gets available sortable properties for a type
    /// </summary>
    public static string[] GetSortableProperties(Type type, bool includeNestedProperties = false, int maxDepth = 2)
    {
        var properties = new List<string>();
        GetSortablePropertiesRecursive(type, string.Empty, properties, includeNestedProperties, maxDepth, 0);
        return properties.ToArray();
    }

    private static void GetSortablePropertiesRecursive(Type type, string prefix, List<string> properties, bool includeNestedProperties, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth)
            return;

        var typeProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsSortableType(p.PropertyType));

        foreach (var prop in typeProperties)
        {
            var propertyPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

            if (IsPrimitiveOrStringType(prop.PropertyType))
            {
                properties.Add(propertyPath);
            }
            else if (includeNestedProperties && currentDepth < maxDepth - 1)
            {
                var actualType = prop.PropertyType;
                if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    actualType = actualType.GetGenericArguments()[0];
                }

                if (!IsPrimitiveOrStringType(actualType) && !IsCollectionType(actualType))
                {
                    GetSortablePropertiesRecursive(actualType, propertyPath, properties, includeNestedProperties, maxDepth, currentDepth + 1);
                }
            }
        }
    }

    private static bool IsSortableType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        return IsPrimitiveOrStringType(type) ||
               !IsCollectionType(type) && type.IsClass && type != typeof(object);
    }

    private static bool IsPrimitiveOrStringType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type == typeof(decimal) ||
               type.IsEnum;
    }

    private static bool IsCollectionType(Type type)
    {
        return type.IsArray ||
               type.IsGenericType && (
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                   type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                   type.GetGenericTypeDefinition() == typeof(IList<>) ||
                   type.GetGenericTypeDefinition() == typeof(List<>)
               ) ||
               type.GetInterfaces().Any(i =>
                   i.IsGenericType &&
                   (i.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    i.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    i.GetGenericTypeDefinition() == typeof(IList<>)));
    }
}
