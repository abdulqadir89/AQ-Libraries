namespace AQ.Utilities.Sort;

/// <summary>
/// Fluent builder for creating sort specifications
/// </summary>
public class SortExpressionBuilder
{
    private readonly List<SortCondition> _conditions = new();
    private int _currentPriority = 0;

    /// <summary>
    /// Adds a sort condition by property path
    /// </summary>
    public SortExpressionBuilder OrderBy(string propertyPath, SortDirection direction = SortDirection.Ascending, bool caseSensitive = false)
    {
        _conditions.Add(new SortCondition
        {
            PropertyPath = propertyPath,
            Direction = direction,
            CaseSensitive = caseSensitive,
            Priority = _currentPriority++
        });
        return this;
    }

    /// <summary>
    /// Adds an ascending sort condition
    /// </summary>
    public SortExpressionBuilder OrderByAscending(string propertyPath, bool caseSensitive = false)
    {
        return OrderBy(propertyPath, SortDirection.Ascending, caseSensitive);
    }

    /// <summary>
    /// Adds a descending sort condition
    /// </summary>
    public SortExpressionBuilder OrderByDescending(string propertyPath, bool caseSensitive = false)
    {
        return OrderBy(propertyPath, SortDirection.Descending, caseSensitive);
    }

    /// <summary>
    /// Adds a sort condition with null handling
    /// </summary>
    public SortExpressionBuilder OrderBy(string propertyPath, SortDirection direction, NullHandling nullHandling, bool caseSensitive = false)
    {
        _conditions.Add(new SortCondition
        {
            PropertyPath = propertyPath,
            Direction = direction,
            CaseSensitive = caseSensitive,
            Priority = _currentPriority++,
            NullHandling = nullHandling
        });
        return this;
    }

    /// <summary>
    /// Adds multiple sort conditions from an expression string
    /// </summary>
    public SortExpressionBuilder FromExpression(string expression)
    {
        if (!string.IsNullOrWhiteSpace(expression))
        {
            var specification = SortExpressionParser.Parse(expression);
            foreach (var condition in specification.GetOrderedConditions())
            {
                condition.Priority = _currentPriority++;
                _conditions.Add(condition);
            }
        }
        return this;
    }

    /// <summary>
    /// Clears all conditions
    /// </summary>
    public SortExpressionBuilder Clear()
    {
        _conditions.Clear();
        _currentPriority = 0;
        return this;
    }

    /// <summary>
    /// Removes sort conditions for a specific property
    /// </summary>
    public SortExpressionBuilder RemoveBy(string propertyPath)
    {
        _conditions.RemoveAll(c => string.Equals(c.PropertyPath, propertyPath, StringComparison.OrdinalIgnoreCase));
        ReorderPriorities();
        return this;
    }

    /// <summary>
    /// Checks if a sort condition exists for the specified property
    /// </summary>
    public bool HasCondition(string propertyPath)
    {
        return _conditions.Any(c => string.Equals(c.PropertyPath, propertyPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the sort direction for a specific property
    /// </summary>
    public SortDirection? GetDirection(string propertyPath)
    {
        var condition = _conditions.FirstOrDefault(c => string.Equals(c.PropertyPath, propertyPath, StringComparison.OrdinalIgnoreCase));
        return condition?.Direction;
    }

    /// <summary>
    /// Builds the final sort specification
    /// </summary>
    public SortSpecification Build()
    {
        return new SortSpecification
        {
            Conditions = new List<SortCondition>(_conditions)
        };
    }

    /// <summary>
    /// Builds and returns the sort expression string
    /// </summary>
    public string BuildExpression()
    {
        var specification = Build();
        return SortExpressionParser.ToExpression(specification);
    }

    /// <summary>
    /// Creates a new builder instance
    /// </summary>
    public static SortExpressionBuilder Create()
    {
        return new SortExpressionBuilder();
    }

    /// <summary>
    /// Creates a builder from an existing expression
    /// </summary>
    public static SortExpressionBuilder CreateFromExpression(string expression)
    {
        return new SortExpressionBuilder().FromExpression(expression);
    }

    private void ReorderPriorities()
    {
        for (int i = 0; i < _conditions.Count; i++)
        {
            _conditions[i].Priority = i;
        }
        _currentPriority = _conditions.Count;
    }
}
