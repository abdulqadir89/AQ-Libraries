namespace AQ.ValueObjects.DataCollection;

public class CollectedValue : ValueObject
{
    public DataType DataType { get; private set; }

    public string? StringValue { get; private set; }
    public decimal? NumericValue { get; private set; }
    public bool? BoolValue { get; private set; }
    public DateTimeOffset? DateTimeOffsetValue { get; private set; }

    private CollectedValue() { } // EF Core

    private CollectedValue(DataType type,
        string? str = null,
        decimal? num = null,
        bool? b = null,
        DateTimeOffset? dto = null)
    {
        DataType = type;
        StringValue = str;
        NumericValue = num;
        BoolValue = b;
        DateTimeOffsetValue = dto;
    }

    public static CollectedValue Create(FieldDefinition definition, object? rawValue)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        if (rawValue == null)
        {
            if (definition.IsRequired)
                throw new ArgumentException($"Field '{definition.Name}' is required but value is null.");
            return new CollectedValue(definition.DataType);
        }

        switch (definition.DataType)
        {
            case DataType.String:
                return new CollectedValue(DataType.String, str: rawValue.ToString());

            case DataType.Numeric:
                if (decimal.TryParse(rawValue.ToString(), out var num))
                    return new CollectedValue(DataType.Numeric, num: num);
                throw new ArgumentException("Invalid numeric value.");

            case DataType.Boolean:
                if (rawValue is bool b)
                    return new CollectedValue(DataType.Boolean, b: b);
                if (bool.TryParse(rawValue.ToString(), out var parsedBool))
                    return new CollectedValue(DataType.Boolean, b: parsedBool);
                throw new ArgumentException("Invalid boolean value.");

            case DataType.DateTimeOffset:
                if (rawValue is DateTimeOffset dto)
                    return new CollectedValue(DataType.DateTimeOffset, dto: dto);
                if (DateTimeOffset.TryParse(rawValue.ToString(), out var parsedDto))
                    return new CollectedValue(DataType.DateTimeOffset, dto: parsedDto);
                throw new ArgumentException("Invalid DateTimeOffset value.");

            case DataType.DateOnly:
                if (rawValue is DateOnly dOnly)
                    return new CollectedValue(DataType.DateOnly, dto: new DateTimeOffset(dOnly.ToDateTime(TimeOnly.MinValue)));
                if (DateOnly.TryParse(rawValue.ToString(), out var parsedDateOnly))
                    return new CollectedValue(DataType.DateOnly, dto: new DateTimeOffset(parsedDateOnly.ToDateTime(TimeOnly.MinValue)));
                throw new ArgumentException("Invalid DateOnly value.");

            case DataType.TimeOnly:
                if (rawValue is TimeOnly tOnly)
                    return new CollectedValue(DataType.TimeOnly, dto: new DateTimeOffset(DateOnly.MinValue.ToDateTime(tOnly)));
                if (TimeOnly.TryParse(rawValue.ToString(), out var parsedTimeOnly))
                    return new CollectedValue(DataType.TimeOnly, dto: new DateTimeOffset(DateOnly.MinValue.ToDateTime(parsedTimeOnly)));
                throw new ArgumentException("Invalid TimeOnly value.");

            case DataType.SingleChoice:
                var choice = rawValue.ToString();
                if (!definition.AllowedValues!.Contains(choice!))
                    throw new ArgumentException($"Value '{choice}' is not in the allowed list.");
                return new CollectedValue(DataType.SingleChoice, str: choice);

            case DataType.MultiChoice:
                var choices = rawValue as IEnumerable<string> ?? rawValue.ToString()!.Split(',');
                if (!choices.All(c => definition.AllowedValues!.Contains(c)))
                    throw new ArgumentException("One or more values are not allowed.");
                return new CollectedValue(DataType.MultiChoice, str: string.Join(",", choices));

            default:
                throw new NotSupportedException($"Data type {definition.DataType} not supported.");
        }
    }

    public object? GetValueObject() => DataType switch
    {
        DataType.String => StringValue,
        DataType.Numeric => NumericValue,
        DataType.Boolean => BoolValue,
        DataType.DateTimeOffset => DateTimeOffsetValue,
        DataType.DateOnly => DateOnly.FromDateTime(DateTimeOffsetValue!.Value.UtcDateTime),
        DataType.TimeOnly => TimeOnly.FromDateTime(DateTimeOffsetValue!.Value.UtcDateTime),
        DataType.SingleChoice => StringValue,
        DataType.MultiChoice => StringValue?.Split(',', StringSplitOptions.RemoveEmptyEntries),
        _ => null
    };

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return DataType;
        yield return StringValue ?? string.Empty;
        yield return NumericValue ?? 0;
        yield return BoolValue ?? false;
        yield return DateTimeOffsetValue ?? DateTimeOffset.MinValue;
    }
}
