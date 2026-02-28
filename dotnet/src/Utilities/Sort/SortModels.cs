namespace AQ.Utilities.Sort;

/// <summary>
/// Represents a single sort condition
/// </summary>
public class SortCondition
{
    /// <summary>
    /// The property path to sort by (supports nested properties like "User.Profile.Name")
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// The sort direction to apply
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// The order priority for multiple sort conditions (lower values have higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Indicates if the sort should be case-sensitive for string properties
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// For handling null values - whether nulls should come first or last
    /// </summary>
    public NullHandling NullHandling { get; set; } = NullHandling.Last;
}

/// <summary>
/// Specifies how null values should be handled in sorting
/// </summary>
public enum NullHandling
{
    /// <summary>
    /// Null values appear first
    /// </summary>
    First,

    /// <summary>
    /// Null values appear last
    /// </summary>
    Last
}

/// <summary>
/// Main sort specification containing all sort criteria
/// </summary>
public class SortSpecification
{
    /// <summary>
    /// List of sort conditions ordered by priority
    /// </summary>
    public List<SortCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Creates a simple sort specification with a single condition
    /// </summary>
    public static SortSpecification Create(string propertyPath, SortDirection direction = SortDirection.Ascending, bool caseSensitive = false)
    {
        return new SortSpecification
        {
            Conditions = new List<SortCondition>
            {
                new SortCondition
                {
                    PropertyPath = propertyPath,
                    Direction = direction,
                    CaseSensitive = caseSensitive,
                    Priority = 0
                }
            }
        };
    }

    /// <summary>
    /// Adds a sort condition to the specification
    /// </summary>
    public SortSpecification ThenBy(string propertyPath, SortDirection direction = SortDirection.Ascending, bool caseSensitive = false)
    {
        var maxPriority = Conditions.Count > 0 ? Conditions.Max(c => c.Priority) : -1;
        Conditions.Add(new SortCondition
        {
            PropertyPath = propertyPath,
            Direction = direction,
            CaseSensitive = caseSensitive,
            Priority = maxPriority + 1
        });
        return this;
    }

    /// <summary>
    /// Gets the conditions ordered by priority
    /// </summary>
    public IEnumerable<SortCondition> GetOrderedConditions()
    {
        return Conditions.OrderBy(c => c.Priority);
    }
}
