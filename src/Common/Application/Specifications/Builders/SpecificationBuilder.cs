using AQ.Common.Application.Specifications.Implementations;
using AQ.Common.Application.Specifications.Interfaces;
using System.Linq.Expressions;

namespace AQ.Common.Application.Specifications.Builders;

/// <summary>
/// Builder class for creating query specifications fluently
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class SpecificationBuilder<T> where T : class
{
    private readonly List<Expression<Func<T, bool>>> _criteria = new();
    private readonly List<string> _includeStrings = new();
    private readonly List<IncludeExpression<T>> _includeExpressions = new();
    private readonly List<(Expression<Func<T, object>> KeySelector, bool IsDescending)> _orderBy = new();
    private int? _take;
    private int? _skip;
    private bool _isPagingEnabled = false;

    /// <summary>
    /// Add a where condition
    /// </summary>
    public SpecificationBuilder<T> Where(Expression<Func<T, bool>> criteria)
    {
        _criteria.Add(criteria);
        return this;
    }

    /// <summary>
    /// Include related entities using string notation
    /// </summary>
    public SpecificationBuilder<T> Include(string includeString)
    {
        _includeStrings.Add(includeString);
        return this;
    }

    /// <summary>
    /// Include related entities using expression
    /// </summary>
    public SpecificationBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> includeExpression)
    {
        _includeExpressions.Add(new IncludeExpression<T>
        {
            Include = includeExpression,
            Type = typeof(TProperty)
        });
        return this;
    }

    /// <summary>
    /// Include related entities and then include nested entities
    /// </summary>
    public SpecificationBuilder<T> ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<T, TPreviousProperty>> previousProperty,
        Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression)
    {
        // For ThenInclude, we need to combine the expressions
        _includeExpressions.Add(new IncludeExpression<T>
        {
            Include = previousProperty,
            Type = typeof(TPreviousProperty),
            ThenInclude = thenIncludeExpression,
            ThenIncludeType = typeof(TProperty)
        });
        return this;
    }

    /// <summary>
    /// Order by ascending
    /// </summary>
    public SpecificationBuilder<T> OrderBy(Expression<Func<T, object>> orderByExpression)
    {
        _orderBy.Add((orderByExpression, false));
        return this;
    }

    /// <summary>
    /// Order by descending
    /// </summary>
    public SpecificationBuilder<T> OrderByDescending(Expression<Func<T, object>> orderByExpression)
    {
        _orderBy.Add((orderByExpression, true));
        return this;
    }

    /// <summary>
    /// Take a specific number of records
    /// </summary>
    public SpecificationBuilder<T> Take(int take)
    {
        _take = take;
        _isPagingEnabled = true;
        return this;
    }

    /// <summary>
    /// Skip a specific number of records
    /// </summary>
    public SpecificationBuilder<T> Skip(int skip)
    {
        _skip = skip;
        _isPagingEnabled = true;
        return this;
    }

    /// <summary>
    /// Enable paging with page number and page size
    /// </summary>
    public SpecificationBuilder<T> Paginate(int pageNumber, int pageSize)
    {
        _skip = (pageNumber - 1) * pageSize;
        _take = pageSize;
        _isPagingEnabled = true;
        return this;
    }

    /// <summary>
    /// Build the final specification
    /// </summary>
    public IQuerySpecification<T> Build()
    {
        return new QuerySpecification<T>
        {
            CriteriaList = _criteria,
            IncludeStrings = _includeStrings,
            IncludeExpressions = _includeExpressions,
            OrderBy = _orderBy.Where(x => !x.IsDescending).Select(x => x.KeySelector).ToList(),
            OrderByDescending = _orderBy.Where(x => x.IsDescending).Select(x => x.KeySelector).ToList(),
            Take = _take,
            Skip = _skip,
            IsPagingEnabled = _isPagingEnabled
        };
    }

    /// <summary>
    /// Build an include-only specification
    /// </summary>
    public IIncludeSpecification<T> BuildIncludeSpec()
    {
        return new IncludeSpecification<T>
        {
            IncludeStringsList = _includeStrings,
            IncludeExpressionsList = _includeExpressions
        };
    }

    /// <summary>
    /// Static method to start building a specification
    /// </summary>
    public static SpecificationBuilder<T> Create() => new();
}
