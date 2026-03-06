namespace AQ.Utilities.Filter;

/// <summary>
/// Represents a single filter condition
/// </summary>
public class FilterCondition
{
    /// <summary>
    /// The property path to filter on (supports nested properties like "User.Profile.Name")
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// The filter operator to apply
    /// </summary>
    public FilterOperator Operator { get; set; }

    /// <summary>
    /// The value(s) to filter with
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// For Between operations, this represents the second value
    /// </summary>
    public object? SecondValue { get; set; }

    /// <summary>
    /// Indicates if the filter should be case-sensitive for string operations
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Represents a group of filter conditions with logical operators
/// </summary>
public class FilterGroup
{
    /// <summary>
    /// Individual filter conditions in this group
    /// </summary>
    public List<FilterCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Nested filter groups
    /// </summary>
    public List<FilterGroup> Groups { get; set; } = new();

    /// <summary>
    /// The logical operator to combine conditions within this group
    /// </summary>
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;

    /// <summary>
    /// Indicates if this group should be negated (NOT)
    /// </summary>
    public bool IsNegated { get; set; } = false;
}

/// <summary>
/// Main filter specification containing all filter criteria
/// </summary>
public class FilterSpecification
{
    /// <summary>
    /// Root filter group containing all filter conditions
    /// </summary>
    public FilterGroup RootGroup { get; set; } = new();

    /// <summary>
    /// Creates a simple filter specification with a single condition
    /// </summary>
    public static FilterSpecification Create(string propertyPath, FilterOperator op, object? value, bool caseSensitive = false)
    {
        return new FilterSpecification
        {
            RootGroup = new FilterGroup
            {
                Conditions = new List<FilterCondition>
                {
                    new FilterCondition
                    {
                        PropertyPath = propertyPath,
                        Operator = op,
                        Value = value,
                        CaseSensitive = caseSensitive
                    }
                }
            }
        };
    }
}
