namespace AQ.Utilities.Search;

/// <summary>
/// Marks a hierarchical entity so global search also matches the listed members of its <c>Parent</c> navigation.
/// Needed because <c>Parent</c> is declared in a base class that cannot carry <see cref="SearchableAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SearchableParentAttribute : Attribute
{
    /// <summary>
    /// Members of the parent to search (e.g. "Name"). Only the direct parent is searched, never its ancestors.
    /// </summary>
    public string[] SubPaths { get; init; } = [];
}
