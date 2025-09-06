namespace AQ.ValueObjects.DataCollection;

public class FieldDefinition : ValueObject
{
    public string Name { get; private set; } = default!;
    public DataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public IReadOnlyList<string>? AllowedValues { get; private set; }

    private FieldDefinition() { } // EF Core

    private FieldDefinition(string name, DataType type, bool isRequired, IEnumerable<string>? allowedValues = null)
    {
        Name = name;
        DataType = type;
        IsRequired = isRequired;
        AllowedValues = allowedValues?.ToList().AsReadOnly();
    }

    public static FieldDefinition Create(string name, DataType type, bool required, IEnumerable<string>? allowedValues = null)
    {
        if ((type == DataType.SingleChoice || type == DataType.MultiChoice) && (allowedValues == null || !allowedValues.Any()))
        {
            throw new ArgumentException("Choice fields must provide allowed values.", nameof(allowedValues));
        }

        return new FieldDefinition(name, type, required, allowedValues);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Name;
        yield return DataType;
        yield return IsRequired;
        if (AllowedValues is not null)
        {
            foreach (var val in AllowedValues)
                yield return val;
        }
    }
}
