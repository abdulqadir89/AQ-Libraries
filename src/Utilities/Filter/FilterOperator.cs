namespace AQ.Utilities.Filter;

/// <summary>
/// Supported filter operators for dynamic filtering
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Equal (=)
    /// </summary>
    Equal,

    /// <summary>
    /// Not equal (!=)
    /// </summary>
    NotEqual,

    /// <summary>
    /// Greater than (>)
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal (>=)
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than (<)
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal (<=)
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Contains (LIKE %value%)
    /// </summary>
    Contains,

    /// <summary>
    /// Does not contain (NOT LIKE %value%)
    /// </summary>
    NotContains,

    /// <summary>
    /// Starts with (LIKE value%)
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with (LIKE %value)
    /// </summary>
    EndsWith,

    /// <summary>
    /// Is null
    /// </summary>
    IsNull,

    /// <summary>
    /// Is not null
    /// </summary>
    IsNotNull,

    /// <summary>
    /// In collection
    /// </summary>
    In,

    /// <summary>
    /// Not in collection
    /// </summary>
    NotIn,

    /// <summary>
    /// Between two values (inclusive)
    /// </summary>
    Between,

    /// <summary>
    /// Not between two values
    /// </summary>
    NotBetween
}
