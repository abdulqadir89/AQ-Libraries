using AQ.Common.Application.Specifications.Interfaces;
using System.Linq.Expressions;

namespace AQ.Common.Application.Specifications.Implementations;

/// <summary>
/// Implementation of include specification for defining related data to include
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class IncludeSpecification<T> : IIncludeSpecification<T> where T : class
{
    private readonly List<Expression<Func<T, object>>> _includes = new();
    private readonly List<string> _includeStrings = new();
    private readonly List<IncludeExpression<T>> _includeExpressions = new();

    public IEnumerable<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();
    public IEnumerable<string> IncludeStrings => _includeStrings.AsReadOnly();
    public IEnumerable<IncludeExpression<T>> IncludeExpressions => _includeExpressions.AsReadOnly();

    /// <summary>
    /// Include strings property for setting from builder
    /// </summary>
    public List<string> IncludeStringsList
    {
        get => _includeStrings;
        set
        {
            _includeStrings.Clear();
            _includeStrings.AddRange(value);
        }
    }

    /// <summary>
    /// Include expressions property for setting from builder
    /// </summary>
    public List<IncludeExpression<T>> IncludeExpressionsList
    {
        get => _includeExpressions;
        set
        {
            _includeExpressions.Clear();
            _includeExpressions.AddRange(value);
        }
    }

    /// <summary>
    /// Add an include expression for a navigation property
    /// </summary>
    /// <param name="includeExpression">Expression pointing to the navigation property</param>
    /// <returns>The specification for chaining</returns>
    public IncludeSpecification<T> Include(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Add an include using string path for complex navigation properties
    /// </summary>
    /// <param name="includePath">String path to the navigation property (e.g., "Orders.OrderItems")</param>
    /// <returns>The specification for chaining</returns>
    public IncludeSpecification<T> Include(string includePath)
    {
        _includeStrings.Add(includePath);
        return this;
    }

    /// <summary>
    /// Create a new empty include specification
    /// </summary>
    /// <returns>A new include specification instance</returns>
    public static IncludeSpecification<T> Create()
    {
        return new IncludeSpecification<T>();
    }
}
