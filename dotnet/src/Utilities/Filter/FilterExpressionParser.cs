using System.Text.RegularExpressions;

namespace AQ.Utilities.Filter;

/// <summary>
/// Parses filter expressions from string format
/// Supports formats like: "Name.FirstName,eq,Jane Doe" or complex expressions with logical operators
/// </summary>
public static class FilterExpressionParser
{
    private static readonly Dictionary<string, FilterOperator> OperatorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "eq", FilterOperator.Equal },
        { "equal", FilterOperator.Equal },
        { "=", FilterOperator.Equal },
        { "ne", FilterOperator.NotEqual },
        { "notequal", FilterOperator.NotEqual },
        { "!=", FilterOperator.NotEqual },
        { "gt", FilterOperator.GreaterThan },
        { "greaterthan", FilterOperator.GreaterThan },
        { ">", FilterOperator.GreaterThan },
        { "gte", FilterOperator.GreaterThanOrEqual },
        { "greaterthanorequal", FilterOperator.GreaterThanOrEqual },
        { ">=", FilterOperator.GreaterThanOrEqual },
        { "lt", FilterOperator.LessThan },
        { "lessthan", FilterOperator.LessThan },
        { "<", FilterOperator.LessThan },
        { "lte", FilterOperator.LessThanOrEqual },
        { "lessthanorequal", FilterOperator.LessThanOrEqual },
        { "<=", FilterOperator.LessThanOrEqual },
        { "contains", FilterOperator.Contains },
        { "like", FilterOperator.Contains },
        { "notcontains", FilterOperator.NotContains },
        { "notlike", FilterOperator.NotContains },
        { "startswith", FilterOperator.StartsWith },
        { "endswith", FilterOperator.EndsWith },
        { "isnull", FilterOperator.IsNull },
        { "null", FilterOperator.IsNull },
        { "isnotnull", FilterOperator.IsNotNull },
        { "notnull", FilterOperator.IsNotNull },
        { "in", FilterOperator.In },
        { "notin", FilterOperator.NotIn },
        { "between", FilterOperator.Between },
        { "notbetween", FilterOperator.NotBetween }
    };

    /// <summary>
    /// Parses a simple filter condition from comma-separated format
    /// Format: "PropertyPath,Operator,Value" or "PropertyPath,Operator,Value1,Value2" (for Between)
    /// </summary>
    public static FilterCondition ParseCondition(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new ArgumentException("Filter expression cannot be null or empty", nameof(filterExpression));

        var parts = SplitRespectingQuotes(filterExpression, ',');

        if (parts.Length < 2)
            throw new ArgumentException($"Invalid filter expression format: {filterExpression}", nameof(filterExpression));

        var propertyPath = parts[0].Trim();
        var operatorStr = parts[1].Trim();

        if (!OperatorMap.TryGetValue(operatorStr, out var filterOperator))
            throw new ArgumentException($"Unknown operator: {operatorStr}", nameof(filterExpression));

        var condition = new FilterCondition
        {
            PropertyPath = propertyPath,
            Operator = filterOperator
        };

        // Handle operators that don't require values
        if (filterOperator == FilterOperator.IsNull || filterOperator == FilterOperator.IsNotNull)
        {
            return condition;
        }

        // Handle operators that require values
        if (parts.Length < 3)
            throw new ArgumentException($"Operator {operatorStr} requires a value", nameof(filterExpression));

        var value = UnquoteValue(parts[2].Trim());
        condition.Value = ParseValue(value);

        // Handle Between operator which requires two values
        if (filterOperator == FilterOperator.Between || filterOperator == FilterOperator.NotBetween)
        {
            if (parts.Length < 4)
                throw new ArgumentException($"Operator {operatorStr} requires two values", nameof(filterExpression));

            var secondValue = UnquoteValue(parts[3].Trim());
            condition.SecondValue = ParseValue(secondValue);
        }

        // Handle In/NotIn operators which can have multiple values
        if (filterOperator == FilterOperator.In || filterOperator == FilterOperator.NotIn)
        {
            var values = new List<object?>();
            for (int i = 2; i < parts.Length; i++)
            {
                values.Add(ParseValue(UnquoteValue(parts[i].Trim())));
            }
            condition.Value = values;
        }

        return condition;
    }

    /// <summary>
    /// Parses multiple filter conditions from an array of filter expressions
    /// </summary>
    public static FilterSpecification ParseConditions(string[] filterExpressions, LogicalOperator logicalOperator = LogicalOperator.And)
    {
        var filterGroup = new FilterGroup
        {
            LogicalOperator = logicalOperator
        };

        foreach (var expression in filterExpressions)
        {
            if (!string.IsNullOrWhiteSpace(expression))
            {
                filterGroup.Conditions.Add(ParseCondition(expression));
            }
        }

        return new FilterSpecification { RootGroup = filterGroup };
    }

    /// <summary>
    /// Parses a complex filter expression with logical operators and parentheses
    /// Example: "(Name,eq,John && Age,gt,25) || (Department,eq,IT)"
    /// </summary>
    public static FilterSpecification ParseComplexExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new FilterSpecification();

        var tokens = TokenizeExpression(expression);
        var rootGroup = ParseTokens(tokens);

        return new FilterSpecification { RootGroup = rootGroup };
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

    private static string UnquoteValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.StartsWith("\"") && value.EndsWith("\"") ||
            value.StartsWith("'") && value.EndsWith("'"))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private static object? ParseValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        // Try to parse as different types
        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        if (int.TryParse(value, out var intValue))
            return intValue;

        if (long.TryParse(value, out var longValue))
            return longValue;

        if (decimal.TryParse(value, out var decimalValue))
            return decimalValue;

        if (double.TryParse(value, out var doubleValue))
            return doubleValue;

        if (DateTime.TryParse(value, out var dateValue))
            return dateValue;

        if (Guid.TryParse(value, out var guidValue))
            return guidValue;

        // Return as string if no other type matches
        return value;
    }

    private static List<string> TokenizeExpression(string expression)
    {
        var tokens = new List<string>();
        var regex = new Regex(@"\(|\)|&&|\|\||[^()&|]+", RegexOptions.IgnoreCase);
        var matches = regex.Matches(expression);

        foreach (Match match in matches)
        {
            var token = match.Value.Trim();
            if (!string.IsNullOrEmpty(token))
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    private static FilterGroup ParseTokens(List<string> tokens)
    {
        var rootGroup = new FilterGroup();
        var currentGroup = rootGroup;
        var groupStack = new Stack<FilterGroup>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token)
            {
                case "(":
                    var newGroup = new FilterGroup();
                    currentGroup.Groups.Add(newGroup);
                    groupStack.Push(currentGroup);
                    currentGroup = newGroup;
                    break;

                case ")":
                    if (groupStack.Count > 0)
                    {
                        currentGroup = groupStack.Pop();
                    }
                    break;

                case "&&":
                    currentGroup.LogicalOperator = LogicalOperator.And;
                    break;

                case "||":
                    currentGroup.LogicalOperator = LogicalOperator.Or;
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        try
                        {
                            var condition = ParseCondition(token);
                            currentGroup.Conditions.Add(condition);
                        }
                        catch
                        {
                            // If parsing as condition fails, ignore the token
                        }
                    }
                    break;
            }
        }

        return rootGroup;
    }
}
