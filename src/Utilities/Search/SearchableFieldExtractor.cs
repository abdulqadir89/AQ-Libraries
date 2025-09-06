using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace AQ.Utilities.Search;

/// <summary>
/// Utilities for extracting searchable fields from entities using reflection and attributes
/// </summary>
public static class SearchableFieldExtractor
{
    /// <summary>
    /// Extracts all searchable fields from a type using SearchableAttribute
    /// </summary>
    /// <typeparam name="T">The type to extract searchable fields from</typeparam>
    /// <param name="includeNestedProperties">Whether to include nested properties</param>
    /// <param name="maxDepth">Maximum depth for nested property traversal</param>
    /// <returns>Dictionary of property paths and their search configurations</returns>
    public static Dictionary<string, SearchFieldInfo> ExtractSearchableFields<T>(bool includeNestedProperties = true, int maxDepth = 3)
    {
        return ExtractSearchableFields(typeof(T), includeNestedProperties, maxDepth);
    }

    /// <summary>
    /// Extracts all searchable fields from a type using SearchableAttribute
    /// </summary>
    /// <param name="type">The type to extract searchable fields from</param>
    /// <param name="includeNestedProperties">Whether to include nested properties</param>
    /// <param name="maxDepth">Maximum depth for nested property traversal</param>
    /// <returns>Dictionary of property paths and their search configurations</returns>
    public static Dictionary<string, SearchFieldInfo> ExtractSearchableFields(Type type, bool includeNestedProperties = true, int maxDepth = 3)
    {
        var fields = new Dictionary<string, SearchFieldInfo>();
        ExtractSearchableFieldsRecursive(type, string.Empty, fields, includeNestedProperties, maxDepth, 0);
        return fields;
    }

    /// <summary>
    /// Gets searchable fields with default search behavior for string properties
    /// </summary>
    /// <typeparam name="T">The type to extract fields from</typeparam>
    /// <param name="includeNestedProperties">Whether to include nested properties</param>
    /// <param name="maxDepth">Maximum depth for nested property traversal</param>
    /// <returns>Dictionary of property paths and their search configurations</returns>
    public static Dictionary<string, SearchFieldInfo> GetDefaultSearchableFields<T>(bool includeNestedProperties = true, int maxDepth = 2)
    {
        return GetDefaultSearchableFields(typeof(T), includeNestedProperties, maxDepth);
    }

    /// <summary>
    /// Gets searchable fields with default search behavior for string properties
    /// </summary>
    /// <param name="type">The type to extract fields from</param>
    /// <param name="includeNestedProperties">Whether to include nested properties</param>
    /// <param name="maxDepth">Maximum depth for nested property traversal</param>
    /// <returns>Dictionary of property paths and their search configurations</returns>
    public static Dictionary<string, SearchFieldInfo> GetDefaultSearchableFields(Type type, bool includeNestedProperties = true, int maxDepth = 2)
    {
        var fields = new Dictionary<string, SearchFieldInfo>();
        GetDefaultSearchableFieldsRecursive(type, string.Empty, fields, includeNestedProperties, maxDepth, 0);
        return fields;
    }

    /// <summary>
    /// Creates a search specification that targets all searchable fields for a given type
    /// </summary>
    /// <typeparam name="T">The type to create search specification for</typeparam>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="searchOperator">The search operator to use</param>
    /// <param name="includeNestedProperties">Whether to include nested properties</param>
    /// <returns>A search specification targeting all searchable fields</returns>
    public static SearchSpecification CreateGlobalSearchSpecification<T>(
        string searchTerm,
        SearchOperator searchOperator = SearchOperator.Contains,
        bool includeNestedProperties = true)
    {
        var searchableFields = ExtractSearchableFields<T>(includeNestedProperties);

        // If no explicit searchable fields found, use default string fields
        if (!searchableFields.Any())
        {
            searchableFields = GetDefaultSearchableFields<T>(includeNestedProperties);
        }

        var conditions = searchableFields.Select(field => new SearchCondition
        {
            PropertyPath = field.Key,
            SearchTerm = searchTerm,
            Operator = searchOperator,
            Weight = field.Value.Weight,
            CaseSensitive = !field.Value.IgnoreCase,
            MinSimilarity = 0.6
        }).ToList();

        var specification = new SearchSpecification
        {
            GlobalSearchTerm = searchTerm,
            RootGroup = new SearchGroup
            {
                Conditions = conditions,
                MatchType = SearchMatchType.Any
            }
        };

        return specification;
    }

    private static void ExtractSearchableFieldsRecursive(
        Type type,
        string prefix,
        Dictionary<string, SearchFieldInfo> fields,
        bool includeNestedProperties,
        int maxDepth,
        int currentDepth)
    {
        if (currentDepth >= maxDepth)
            return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsSearchableProperty(p));

        foreach (var property in properties)
        {
            var searchableAttribute = property.GetCustomAttribute<SearchableAttribute>();
            if (searchableAttribute == null)
                continue;

            var propertyPath = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            var fieldName = searchableAttribute.SearchFieldName ?? property.Name;

            fields[propertyPath] = new SearchFieldInfo
            {
                PropertyPath = propertyPath,
                FieldName = fieldName,
                PropertyType = property.PropertyType,
                Weight = searchableAttribute.Weight,
                EnableFuzzyMatch = searchableAttribute.EnableFuzzyMatch,
                EnableExactMatch = searchableAttribute.EnableExactMatch,
                EnablePrefixMatch = searchableAttribute.EnablePrefixMatch,
                MinSearchLength = searchableAttribute.MinSearchLength,
                IgnoreCase = searchableAttribute.IgnoreCase
            };

            // Recursively process nested properties if enabled
            if (includeNestedProperties && currentDepth < maxDepth - 1)
            {
                var actualType = property.PropertyType;
                if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    actualType = actualType.GetGenericArguments()[0];
                }

                if (IsComplexType(actualType) && !IsCollectionType(actualType))
                {
                    ExtractSearchableFieldsRecursive(actualType, propertyPath, fields, includeNestedProperties, maxDepth, currentDepth + 1);
                }
            }
        }
    }

    private static void GetDefaultSearchableFieldsRecursive(
        Type type,
        string prefix,
        Dictionary<string, SearchFieldInfo> fields,
        bool includeNestedProperties,
        int maxDepth,
        int currentDepth)
    {
        if (currentDepth >= maxDepth)
            return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsSearchableType(p.PropertyType) && IsSearchableProperty(p));

        foreach (var property in properties)
        {
            var propertyPath = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            fields[propertyPath] = new SearchFieldInfo
            {
                PropertyPath = propertyPath,
                FieldName = property.Name,
                PropertyType = property.PropertyType,
                Weight = 1.0,
                EnableFuzzyMatch = property.PropertyType == typeof(string),
                EnableExactMatch = true,
                EnablePrefixMatch = property.PropertyType == typeof(string),
                MinSearchLength = 1,
                IgnoreCase = property.PropertyType == typeof(string)
            };

            // Recursively process nested properties if enabled
            if (includeNestedProperties && currentDepth < maxDepth - 1)
            {
                var actualType = property.PropertyType;
                if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    actualType = actualType.GetGenericArguments()[0];
                }

                if (IsComplexType(actualType) && !IsCollectionType(actualType))
                {
                    GetDefaultSearchableFieldsRecursive(actualType, propertyPath, fields, includeNestedProperties, maxDepth, currentDepth + 1);
                }
            }
        }
    }

    /// <summary>
    /// Determines if a property should be included in search operations.
    /// Excludes computed properties, NotMapped properties, and other non-searchable properties.
    /// Properties with explicit [Searchable] attribute are always included.
    /// </summary>
    /// <param name="property">The property to check</param>
    /// <returns>True if the property should be searchable, false otherwise</returns>
    private static bool IsSearchableProperty(PropertyInfo property)
    {
        // Skip properties with NotMapped attribute
        if (property.GetCustomAttribute<NotMappedAttribute>() != null)
            return false;

        // Properties with explicit [Searchable] attribute are always included
        if (property.GetCustomAttribute<SearchableAttribute>() != null)
            return true;

        // Skip properties that are computed (expression-bodied properties with no setter)
        if (IsComputedProperty(property))
            return false;

        // Skip collection navigation properties (they usually have backing fields)
        if (IsCollectionType(property.PropertyType))
            return false;

        return true;
    }

    /// <summary>
    /// Determines if a property is a computed property (expression-bodied property with no setter).
    /// This is a heuristic check that covers most common computed property patterns.
    /// </summary>
    /// <param name="property">The property to check</param>
    /// <returns>True if the property appears to be computed, false otherwise</returns>
    private static bool IsComputedProperty(PropertyInfo property)
    {
        // If there's no setter, it might be computed
        if (!property.CanWrite)
            return true;

        // If the setter is private and getter exists, it might be computed
        var setMethod = property.GetSetMethod(true); // Include non-public setters
        if (setMethod != null && setMethod.IsPrivate && property.CanRead)
        {
            // Additional check: if the getter has a very simple implementation pattern
            // This is a heuristic - we can't easily detect expression body syntax at runtime
            // but we can make educated guesses based on naming patterns and return types

            // For properties like "FullName" that combine other properties, 
            // they often follow naming patterns that indicate computed nature
            if (property.Name.Contains("Full") ||
                property.Name.Contains("Display") ||
                property.Name.Contains("Computed") ||
                property.Name.Contains("Combined") ||
                property.Name.EndsWith("Text") ||
                property.Name.EndsWith("Label"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSearchableType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        return type == typeof(string) ||
               type.IsPrimitive ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type == typeof(decimal) ||
               type.IsEnum;
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive &&
               type != typeof(string) &&
               type != typeof(DateTime) &&
               type != typeof(DateTimeOffset) &&
               type != typeof(TimeSpan) &&
               type != typeof(Guid) &&
               type != typeof(decimal) &&
               !type.IsEnum &&
               type != typeof(object);
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

/// <summary>
/// Information about a searchable field
/// </summary>
public class SearchFieldInfo
{
    /// <summary>
    /// Property path (e.g., "User.Profile.Name")
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// Field name for searching
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the property
    /// </summary>
    public Type PropertyType { get; set; } = typeof(object);

    /// <summary>
    /// Weight/importance of this field in search results
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Whether fuzzy matching is enabled for this field
    /// </summary>
    public bool EnableFuzzyMatch { get; set; } = true;

    /// <summary>
    /// Whether exact matching is enabled for this field
    /// </summary>
    public bool EnableExactMatch { get; set; } = true;

    /// <summary>
    /// Whether prefix matching is enabled for this field
    /// </summary>
    public bool EnablePrefixMatch { get; set; } = true;

    /// <summary>
    /// Minimum search term length required for this field
    /// </summary>
    public int MinSearchLength { get; set; } = 1;

    /// <summary>
    /// Whether to ignore case when searching this field
    /// </summary>
    public bool IgnoreCase { get; set; } = true;
}
