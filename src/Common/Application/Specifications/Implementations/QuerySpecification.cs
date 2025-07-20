using AQ.Common.Application.Specifications.Interfaces;
using System.Linq.Expressions;

namespace AQ.Common.Application.Specifications.Implementations;

/// <summary>
/// Implementation of IQuerySpecification for querying entities
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class QuerySpecification<T> : IQuerySpecification<T> where T : class
{
    /// <summary>
    /// Combined filter criteria (all criteria will be AND-ed together)
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; set; }

    /// <summary>
    /// List of criteria expressions that will be combined with AND
    /// </summary>
    public List<Expression<Func<T, bool>>> CriteriaList { get; set; } = new();

    /// <summary>
    /// Order by expressions
    /// </summary>
    public List<Expression<Func<T, object>>> OrderBy { get; set; } = new();

    /// <summary>
    /// Order by descending expressions
    /// </summary>
    public List<Expression<Func<T, object>>> OrderByDescending { get; set; } = new();

    /// <summary>
    /// Number of records to take (for paging)
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// Number of records to skip (for paging)
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Whether paging is enabled
    /// </summary>
    public bool IsPagingEnabled { get; set; }

    /// <summary>
    /// Include strings for related entities
    /// </summary>
    public List<string> IncludeStrings { get; set; } = new();

    /// <summary>
    /// Include expressions for related entities
    /// </summary>
    public List<IncludeExpression<T>> IncludeExpressions { get; set; } = new();

    /// <summary>
    /// Implementation of IIncludeSpecification.Includes (computed from IncludeExpressions)
    /// </summary>
    IEnumerable<Expression<Func<T, object>>> IIncludeSpecification<T>.Includes =>
        IncludeExpressions.Select(ie => (Expression<Func<T, object>>)ie.Include);

    /// <summary>
    /// Implementation of IIncludeSpecification.IncludeStrings
    /// </summary>
    IEnumerable<string> IIncludeSpecification<T>.IncludeStrings => IncludeStrings;

    /// <summary>
    /// Implementation of IIncludeSpecification.IncludeExpressions
    /// </summary>
    IEnumerable<IncludeExpression<T>> IIncludeSpecification<T>.IncludeExpressions => IncludeExpressions;

    /// <summary>
    /// Combined criteria expression (computed property)
    /// </summary>
    Expression<Func<T, bool>>? IQuerySpecification<T>.Criteria
    {
        get
        {
            if (Criteria != null && CriteriaList.Any())
            {
                // Combine the single criteria with the list
                return CombineExpressions(new[] { Criteria }.Concat(CriteriaList));
            }
            else if (Criteria != null)
            {
                return Criteria;
            }
            else if (CriteriaList.Any())
            {
                return CombineExpressions(CriteriaList);
            }
            return null;
        }
    }

    /// <summary>
    /// Combines multiple expressions with AND logic
    /// </summary>
    private Expression<Func<T, bool>> CombineExpressions(IEnumerable<Expression<Func<T, bool>>> expressions)
    {
        var expressionList = expressions.ToList();
        if (!expressionList.Any())
            throw new ArgumentException("Cannot combine empty expression list");

        if (expressionList.Count == 1)
            return expressionList.First();

        var combined = expressionList.First();
        foreach (var expression in expressionList.Skip(1))
        {
            combined = CombineAnd(combined, expression);
        }
        return combined;
    }

    /// <summary>
    /// Combines two expressions with AND logic
    /// </summary>
    private Expression<Func<T, bool>> CombineAnd(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        var combinedBody = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    /// <summary>
    /// Replaces parameter in expression
    /// </summary>
    private Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    /// <summary>
    /// Expression visitor for parameter replacement
    /// </summary>
    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
