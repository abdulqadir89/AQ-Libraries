using System.Linq.Expressions;

namespace AQ.Common.Application.Specifications.Interfaces;

/// <summary>
/// Specification pattern for querying entities with filtering, ordering, and includes
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IQuerySpecification<T> : IIncludeSpecification<T> where T : class
{
    /// <summary>
    /// Filter criteria for the query
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Order by expressions
    /// </summary>
    List<Expression<Func<T, object>>> OrderBy { get; }

    /// <summary>
    /// Order by descending expressions
    /// </summary>
    List<Expression<Func<T, object>>> OrderByDescending { get; }

    /// <summary>
    /// Number of records to take (for paging)
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Number of records to skip (for paging)
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Whether paging is enabled
    /// </summary>
    bool IsPagingEnabled { get; }
}
