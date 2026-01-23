namespace AQ.ValueObjects;

/// <summary>
/// Represents a value object that validates a value against a numerical range.
/// Generic implementation supporting any numeric type.
/// </summary>
/// <typeparam name="T">The numeric type (int, float, decimal, double, etc.)</typeparam>
public sealed class RangeValidatedValue<T> : ValueObject where T : struct, IComparable<T>, IComparable
{
    public T Value { get; init; }

    private RangeValidatedValue() { }

    private RangeValidatedValue(NumericalRange<T> range, T value)
    {
        if (!range.Contains(value))
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is not within the allowed range {range}.");
        Value = value;
    }

    /// <summary>
    /// Creates a new RangeValidatedValue instance with the specified range and value.
    /// </summary>
    /// <param name="range">The numerical range to validate against.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>A valid RangeValidatedValue instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the range.</exception>
    public static RangeValidatedValue<T> Create(NumericalRange<T> range, T value) => new(range, value);

    /// <summary>
    /// Attempts to create a new RangeValidatedValue instance without throwing an exception.
    /// </summary>
    /// <param name="range">The numerical range to validate against.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="result">The created RangeValidatedValue instance if successful.</param>
    /// <returns>True if the value is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(NumericalRange<T> range, T value, out RangeValidatedValue<T>? result)
    {
        try
        {
            result = new RangeValidatedValue<T>(range, value);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString() ?? "";

    public override RangeValidatedValue<T> Clone()
    {
        return new RangeValidatedValue<T>
        {
            Value = Value,
        };
    }
}
