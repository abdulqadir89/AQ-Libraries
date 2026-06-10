namespace AQ.Utilities.Search;

/// <summary>
/// Attribute to mark properties as searchable for fuzzy search
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SearchableAttribute : Attribute
{
    /// <summary>
    /// The weight/priority of this field in search results (higher = more important)
    /// </summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>
    /// Whether this field should be included in fuzzy matching
    /// </summary>
    public bool EnableFuzzyMatch { get; init; } = true;

    /// <summary>
    /// Whether this field should be included in exact/partial matching
    /// </summary>
    public bool EnableExactMatch { get; init; } = true;

    /// <summary>
    /// Whether this field should be included in prefix matching
    /// </summary>
    public bool EnablePrefixMatch { get; init; } = true;

    /// <summary>
    /// Custom field name for searching (if different from property name)
    /// </summary>
    public string? SearchFieldName { get; init; }

    /// <summary>
    /// Minimum search term length required for this field to be considered
    /// </summary>
    public int MinSearchLength { get; init; } = 1;

    /// <summary>
    /// Whether to ignore case when searching this field
    /// </summary>
    public bool IgnoreCase { get; init; } = true;

    /// <summary>
    /// For complex-type properties, explicitly specifies one or more sub-property paths to search
    /// (e.g., new[] { "Value" } for MarkdownContent). When set, skips recursion and registers each
    /// path as its own leaf field. All paths inherit the weight and options of the parent attribute.
    /// </summary>
    public string[]? SubPaths { get; init; }
}
