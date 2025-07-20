using System.Linq.Expressions;

namespace AQ.Common.Application.Specifications.Interfaces;

/// <summary>
/// Represents a specification for including related data without directly depending on EF Core
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IIncludeSpecification<T> where T : class
{
    /// <summary>
    /// Gets the include expressions for the entity
    /// </summary>
    IEnumerable<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the string-based include paths for complex navigation properties
    /// </summary>
    IEnumerable<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the complex include expressions with type information
    /// </summary>
    IEnumerable<IncludeExpression<T>> IncludeExpressions { get; }
}
