namespace AQ.Utilities.Filter;

/// <summary>
/// Extension methods for easier filtering
/// </summary>
public static class FilterExtensions
{
    /// <summary>
    /// Applies a simple filter condition to the query
    /// </summary>
    public static IQueryable<T> Filter<T>(this IQueryable<T> query, string propertyPath, FilterOperator op, object? value, bool caseSensitive = false)
    {
        var filterSpec = FilterSpecification.Create(propertyPath, op, value, caseSensitive);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies multiple filter conditions with AND logic
    /// </summary>
    public static IQueryable<T> FilterAnd<T>(this IQueryable<T> query, params string[] filterExpressions)
    {
        var filterSpec = FilterExpressionParser.ParseConditions(filterExpressions, LogicalOperator.And);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies multiple filter conditions with OR logic
    /// </summary>
    public static IQueryable<T> FilterOr<T>(this IQueryable<T> query, params string[] filterExpressions)
    {
        var filterSpec = FilterExpressionParser.ParseConditions(filterExpressions, LogicalOperator.Or);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies a complex filter expression
    /// </summary>
    public static IQueryable<T> FilterComplex<T>(this IQueryable<T> query, string complexExpression)
    {
        var filterSpec = FilterExpressionParser.ParseComplexExpression(complexExpression);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies filters from a comma-separated filter string
    /// </summary>
    public static IQueryable<T> FilterFromString<T>(this IQueryable<T> query, string? filterString, LogicalOperator logicalOperator = LogicalOperator.And)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return query;

        var expressions = filterString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var filterSpec = FilterExpressionParser.ParseConditions(expressions, logicalOperator);
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Applies filters from a dictionary of property-value pairs
    /// </summary>
    public static IQueryable<T> FilterFromDictionary<T>(this IQueryable<T> query,
        Dictionary<string, object?> filters,
        FilterOperator defaultOperator = FilterOperator.Equal,
        LogicalOperator logicalOperator = LogicalOperator.And)
    {
        if (!filters.Any())
            return query;

        var filterGroup = new FilterGroup { LogicalOperator = logicalOperator };

        foreach (var filter in filters)
        {
            if (filter.Value != null)
            {
                filterGroup.Conditions.Add(new FilterCondition
                {
                    PropertyPath = filter.Key,
                    Operator = defaultOperator,
                    Value = filter.Value
                });
            }
        }

        var filterSpec = new FilterSpecification { RootGroup = filterGroup };
        return query.ApplyFilter(filterSpec);
    }

    /// <summary>
    /// Creates a filter specification builder for fluent syntax
    /// </summary>
    public static FilterSpecificationBuilder<T> CreateFilter<T>(this IQueryable<T> query)
    {
        return new FilterSpecificationBuilder<T>(query);
    }
}

/// <summary>
/// Fluent builder for filter specifications
/// </summary>
public class FilterSpecificationBuilder<T>
{
    private readonly IQueryable<T> _query;
    private readonly FilterGroup _currentGroup;
    private readonly Stack<FilterGroup> _groupStack;

    internal FilterSpecificationBuilder(IQueryable<T> query)
    {
        _query = query;
        _currentGroup = new FilterGroup();
        _groupStack = new Stack<FilterGroup>();
    }

    /// <summary>
    /// Adds a filter condition
    /// </summary>
    public FilterSpecificationBuilder<T> Where(string propertyPath, FilterOperator op, object? value, bool caseSensitive = false)
    {
        _currentGroup.Conditions.Add(new FilterCondition
        {
            PropertyPath = propertyPath,
            Operator = op,
            Value = value,
            CaseSensitive = caseSensitive
        });
        return this;
    }

    /// <summary>
    /// Adds an equal condition
    /// </summary>
    public FilterSpecificationBuilder<T> Equal(string propertyPath, object? value, bool caseSensitive = false)
    {
        return Where(propertyPath, FilterOperator.Equal, value, caseSensitive);
    }

    /// <summary>
    /// Adds a contains condition
    /// </summary>
    public FilterSpecificationBuilder<T> Contains(string propertyPath, string? value, bool caseSensitive = false)
    {
        return Where(propertyPath, FilterOperator.Contains, value, caseSensitive);
    }

    /// <summary>
    /// Adds a greater than condition
    /// </summary>
    public FilterSpecificationBuilder<T> GreaterThan(string propertyPath, object? value)
    {
        return Where(propertyPath, FilterOperator.GreaterThan, value);
    }

    /// <summary>
    /// Adds a less than condition
    /// </summary>
    public FilterSpecificationBuilder<T> LessThan(string propertyPath, object? value)
    {
        return Where(propertyPath, FilterOperator.LessThan, value);
    }

    /// <summary>
    /// Adds an In condition
    /// </summary>
    public FilterSpecificationBuilder<T> In(string propertyPath, IEnumerable<object?> values)
    {
        return Where(propertyPath, FilterOperator.In, values);
    }

    /// <summary>
    /// Adds a between condition
    /// </summary>
    public FilterSpecificationBuilder<T> Between(string propertyPath, object? value1, object? value2)
    {
        _currentGroup.Conditions.Add(new FilterCondition
        {
            PropertyPath = propertyPath,
            Operator = FilterOperator.Between,
            Value = value1,
            SecondValue = value2
        });
        return this;
    }

    /// <summary>
    /// Sets the logical operator for the current group
    /// </summary>
    public FilterSpecificationBuilder<T> And()
    {
        _currentGroup.LogicalOperator = LogicalOperator.And;
        return this;
    }

    /// <summary>
    /// Sets the logical operator for the current group
    /// </summary>
    public FilterSpecificationBuilder<T> Or()
    {
        _currentGroup.LogicalOperator = LogicalOperator.Or;
        return this;
    }

    /// <summary>
    /// Starts a new group
    /// </summary>
    public FilterSpecificationBuilder<T> BeginGroup()
    {
        var newGroup = new FilterGroup();
        _currentGroup.Groups.Add(newGroup);
        _groupStack.Push(_currentGroup);
        return this;
    }

    /// <summary>
    /// Ends the current group
    /// </summary>
    public FilterSpecificationBuilder<T> EndGroup()
    {
        if (_groupStack.Count > 0)
        {
            _groupStack.Pop();
        }
        return this;
    }

    /// <summary>
    /// Builds and applies the filter to the query
    /// </summary>
    public IQueryable<T> Build()
    {
        var filterSpec = new FilterSpecification { RootGroup = _currentGroup };
        return _query.ApplyFilter(filterSpec);
    }
}
