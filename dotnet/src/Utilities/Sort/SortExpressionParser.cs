namespace AQ.Utilities.Sort;

/// <summary>
/// Parses sort expressions from string format
/// Supports formats like: "Name,asc" or complex expressions with multiple sorts: "Parent.Name,asc;Description,desc"
/// </summary>
public static class SortExpressionParser
{
    private static readonly Dictionary<string, SortDirection> DirectionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "asc", SortDirection.Ascending },
        { "ascending", SortDirection.Ascending },
        { "asce", SortDirection.Ascending }, // Support the typo in your example
        { "up", SortDirection.Ascending },
        { "desc", SortDirection.Descending },
        { "descending", SortDirection.Descending },
        { "down", SortDirection.Descending }
    };

    /// <summary>
    /// Parses a simple sort condition from comma-separated format
    /// Format: "PropertyPath,Direction" or just "PropertyPath" (defaults to ascending)
    /// </summary>
    public static SortCondition ParseCondition(string sortExpression)
    {
        if (string.IsNullOrWhiteSpace(sortExpression))
            throw new ArgumentException("Sort expression cannot be null or empty", nameof(sortExpression));

        var parts = SplitRespectingQuotes(sortExpression, ',');

        if (parts.Length < 1)
            throw new ArgumentException($"Invalid sort expression format: {sortExpression}", nameof(sortExpression));

        var propertyPath = parts[0].Trim();
        var direction = SortDirection.Ascending; // Default

        if (parts.Length > 1)
        {
            var directionStr = parts[1].Trim();
            if (!DirectionMap.TryGetValue(directionStr, out direction))
                throw new ArgumentException($"Unknown sort direction: {directionStr}", nameof(sortExpression));
        }

        return new SortCondition
        {
            PropertyPath = propertyPath,
            Direction = direction
        };
    }

    /// <summary>
    /// Parses multiple sort conditions from an array of sort expressions
    /// </summary>
    public static SortSpecification ParseConditions(string[] sortExpressions)
    {
        var specification = new SortSpecification();

        for (int i = 0; i < sortExpressions.Length; i++)
        {
            var expression = sortExpressions[i];
            if (!string.IsNullOrWhiteSpace(expression))
            {
                var condition = ParseCondition(expression);
                condition.Priority = i; // Set priority based on order
                specification.Conditions.Add(condition);
            }
        }

        return specification;
    }

    /// <summary>
    /// Parses a complex sort expression with multiple conditions separated by semicolons
    /// Example: "Parent.Name,asc;Description,desc;CreatedDate,desc"
    /// </summary>
    public static SortSpecification ParseComplexExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new SortSpecification();

        // Split by semicolon to get individual sort conditions
        var conditions = SplitRespectingQuotes(expression, ';');
        return ParseConditions(conditions);
    }

    /// <summary>
    /// Parses sort expression supporting both simple and complex formats
    /// - Simple: "Name,asc" or "Name"
    /// - Complex: "Parent.Name,asc;Description,desc"
    /// </summary>
    public static SortSpecification Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new SortSpecification();

        // Check if it's a complex expression (contains semicolon)
        if (expression.Contains(';'))
        {
            return ParseComplexExpression(expression);
        }

        // Simple single condition
        var condition = ParseCondition(expression);
        return new SortSpecification
        {
            Conditions = new List<SortCondition> { condition }
        };
    }

    /// <summary>
    /// Converts a SortSpecification back to a string expression
    /// </summary>
    public static string ToExpression(SortSpecification specification)
    {
        if (specification?.Conditions == null || !specification.Conditions.Any())
            return string.Empty;

        var conditions = specification.GetOrderedConditions()
            .Select(c => $"{c.PropertyPath},{DirectionToString(c.Direction)}");

        return string.Join(";", conditions);
    }

    /// <summary>
    /// Validates if a sort expression is valid
    /// </summary>
    public static bool IsValidExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true; // Empty is valid (no sorting)

        try
        {
            Parse(expression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all property paths from a sort expression
    /// </summary>
    public static string[] GetPropertyPaths(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return Array.Empty<string>();

        try
        {
            var specification = Parse(expression);
            return specification.GetOrderedConditions()
                .Select(c => c.PropertyPath)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string[] SplitRespectingQuotes(string input, char delimiter)
    {
        var result = new List<string>();
        var current = new List<char>();
        bool inQuotes = false;
        char quoteChar = '"';

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '"' || c == '\'')
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar)
                {
                    inQuotes = false;
                }
                current.Add(c);
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(new string(current.ToArray()));
                current.Clear();
            }
            else
            {
                current.Add(c);
            }
        }

        if (current.Count > 0)
        {
            result.Add(new string(current.ToArray()));
        }

        return result.ToArray();
    }

    private static string DirectionToString(SortDirection direction)
    {
        return direction == SortDirection.Ascending ? "asc" : "desc";
    }
}
