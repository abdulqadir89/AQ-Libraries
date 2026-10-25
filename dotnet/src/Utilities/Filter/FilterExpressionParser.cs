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
    // RULE FLT-1
    public static FilterCondition ParseCondition(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new ArgumentException("Filter expression cannot be null or empty", nameof(filterExpression));

        var parts = SplitRespectingQuotes(filterExpression, ',');

        if (parts.Length < 2)
            throw new ArgumentException($"Invalid filter expression format: {filterExpression}", nameof(filterExpression));

        var propertyPath = parts[0].Trim();
        var operatorStr = parts[1].Trim();

        // RULE FLT-2
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
    /// Parses a complex filter expression with logical operators, parentheses and negation
    /// Example: "(Name,eq,John && Age,gt,25) || !(Department,eq,IT)"; && binds tighter than ||
    /// </summary>
    public static FilterSpecification ParseComplexExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new FilterSpecification();

        var reader = new ExpressionReader(TokenizeExpression(expression));
        return new FilterSpecification { RootGroup = reader.ReadRoot() };
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

    // A quote opens a quoted value only straight after a comma, so an apostrophe inside a word (O'Brien) stays plain text.
    private static List<string> TokenizeExpression(string expression)
    {
        var tokens = new List<string>();
        var text = new System.Text.StringBuilder();
        var quote = '\0';

        void Flush()
        {
            var token = text.ToString().Trim();
            if (token.Length > 0)
                tokens.Add(token);
            text.Clear();
        }

        for (var i = 0; i < expression.Length; i++)
        {
            var c = expression[i];

            if (quote != '\0')
            {
                text.Append(c);
                if (c == quote)
                    quote = '\0';
                continue;
            }

            // RULE FLT-8
            if ((c == '"' || c == '\'') && text.Length > 0 && text[^1] == ',')
            {
                quote = c;
                text.Append(c);
                continue;
            }

            if (c == '(' || c == ')')
            {
                Flush();
                tokens.Add(c.ToString());
            }
            else if (c == '&' && i + 1 < expression.Length && expression[i + 1] == '&')
            {
                Flush();
                tokens.Add("&&");
                i++;
            }
            else if (c == '|' && i + 1 < expression.Length && expression[i + 1] == '|')
            {
                Flush();
                tokens.Add("||");
                i++;
            }
            else if (c == '!' && text.ToString().Trim().Length == 0 && NextNonSpace(expression, i + 1) is '(' or '!')
            {
                tokens.Add("!");
            }
            else
            {
                text.Append(c);
            }
        }

        Flush();
        return tokens;
    }

    private static char NextNonSpace(string expression, int from)
    {
        for (var i = from; i < expression.Length; i++)
        {
            if (!char.IsWhiteSpace(expression[i]))
                return expression[i];
        }

        return '\0';
    }

    // Grammar: or := and ('||' and)* ; and := unary ('&&' unary)* ; unary := '!' unary | '(' or ')' | condition
    // RULE FLT-3
    private sealed class ExpressionReader(List<string> tokens)
    {
        // Each bracket and ! recurses once, so the depth is capped well below what the stack can take.
        private const int MaxDepth = 32;

        private int _position;
        private int _depth;

        private string? Peek => _position < tokens.Count ? tokens[_position] : null;

        public FilterGroup ReadRoot()
        {
            var node = ReadOr();
            if (node is null)
                return new FilterGroup();

            // A condition, a negated group or a bracketed group sits inside the root; a plain and/or group is the root.
            if (node.Condition is not null)
                return new FilterGroup { Conditions = { node.Condition } };

            return node.Group!.IsNegated ? new FilterGroup { Groups = { node.Group } } : node.Group;
        }

        private Node? ReadOr() => ReadChain(ReadAnd, "||", LogicalOperator.Or);

        private Node? ReadAnd() => ReadChain(ReadUnary, "&&", LogicalOperator.And);

        private Node? ReadChain(Func<Node?> readOperand, string separator, LogicalOperator logicalOperator)
        {
            var operands = new List<Node>();
            var first = readOperand();
            if (first is not null)
                operands.Add(first);

            while (Peek == separator)
            {
                _position++;
                var next = readOperand();
                if (next is not null)
                    operands.Add(next);
            }

            if (operands.Count == 0)
                return null;
            if (operands.Count == 1)
                return operands[0];

            var group = new FilterGroup { LogicalOperator = logicalOperator };
            foreach (var operand in operands)
                operand.AddTo(group);
            return new Node(null, group);
        }

        private Node? ReadUnary()
        {
            var token = Peek;
            if (token is null)
                return null;

            if (token is "!" or "(")
                EnterNesting();

            try
            {
                return ReadNested(token);
            }
            finally
            {
                if (token is "!" or "(")
                    _depth--;
            }
        }

        // RULE FLT-2
        private void EnterNesting()
        {
            if (++_depth > MaxDepth)
                throw new ArgumentException($"A filter expression cannot nest deeper than {MaxDepth} levels.");
        }

        private Node? ReadNested(string token)
        {
            if (token == "!")
            {
                _position++;
                var operand = ReadUnary();
                if (operand is null)
                    return null;

                var negated = new FilterGroup { IsNegated = true };
                operand.AddTo(negated);
                return new Node(null, negated);
            }

            if (token == "(")
            {
                _position++;
                var inner = ReadOr();
                if (Peek == ")")
                    _position++;
                return inner;
            }

            if (token is ")" or "&&" or "||")
                return null;

            _position++;
            try
            {
                return new Node(ParseCondition(token), null);
            }
            catch (ArgumentException)
            {
                // RULE FLT-4
                return null;
            }
        }
    }

    private sealed record Node(FilterCondition? Condition, FilterGroup? Group)
    {
        public void AddTo(FilterGroup parent)
        {
            if (Condition is not null)
                parent.Conditions.Add(Condition);
            else
                parent.Groups.Add(Group!);
        }
    }
}
