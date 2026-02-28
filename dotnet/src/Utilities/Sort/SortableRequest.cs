namespace AQ.Utilities.Sort;

/// <summary>
/// Base interface for requests that support sorting
/// </summary>
public interface ISortableRequest
{
    /// <summary>
    /// Complex sort expression supporting multiple fields with directions
    /// Examples:
    /// - Simple: "Name,asc"
    /// - Multiple: "Parent.Name,asc;Description,desc;CreatedDate,desc"
    /// Format: "PropertyPath,Direction;PropertyPath,Direction"
    /// Supported directions: asc, desc, ascending, descending
    /// </summary>
    string? SortExpression { get; }
}

/// <summary>
/// Default implementation of sortable request
/// </summary>
public record SortableRequest : ISortableRequest
{
    /// <summary>
    /// Complex sort expression supporting multiple fields with directions
    /// Examples:
    /// - Simple: "Name,asc"
    /// - Multiple: "Parent.Name,asc;Description,desc;CreatedDate,desc"
    /// Format: "PropertyPath,Direction;PropertyPath,Direction"
    /// Supported directions: asc, desc, ascending, descending
    /// </summary>
    public string? SortExpression { get; init; }
}
