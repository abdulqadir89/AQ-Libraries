using System.Linq.Expressions;

namespace AQ.Common.Application.Specifications.Interfaces;

/// <summary>
/// Represents an include expression for entity loading
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class IncludeExpression<T> where T : class
{
    /// <summary>
    /// The include expression
    /// </summary>
    public LambdaExpression Include { get; set; } = null!;

    /// <summary>
    /// The type of the included property
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// Optional then include expression for nested includes
    /// </summary>
    public LambdaExpression? ThenInclude { get; set; }

    /// <summary>
    /// The type of the then included property
    /// </summary>
    public Type? ThenIncludeType { get; set; }
}
