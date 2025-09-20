namespace AQ.ValueObjects;

/// <summary>
/// Represents a numerical range value object with validation. Supports open-ended ranges.
/// </summary>
/// <typeparam name="T">The numeric type (int, decimal, double, etc.)</typeparam>
public sealed class NumericalRange<T> : ValueObject where T : struct, IComparable<T>, IComparable
{
    public T? Min { get; init; }
    public T? Max { get; init; }

    // Parameterless constructor for EF Core
    public NumericalRange() { }

    private NumericalRange(T? min, T? max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates a new NumericalRange instance with validation.
    /// </summary>
    /// <param name="min">The minimum value of the range (null for open-ended).</param>
    /// <param name="max">The maximum value of the range (null for open-ended).</param>
    /// <returns>A valid NumericalRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the maximum value is less than the minimum value.</exception>
    public static NumericalRange<T> Create(T? min, T? max)
    {
        if (min.HasValue && max.HasValue && max.Value.CompareTo(min.Value) < 0)
            throw new ArgumentException("Maximum value cannot be less than minimum value.", nameof(max));

        return new NumericalRange<T>(min, max);
    }

    /// <summary>
    /// Creates a closed numerical range with both minimum and maximum values.
    /// </summary>
    /// <param name="min">The minimum value of the range.</param>
    /// <param name="max">The maximum value of the range.</param>
    /// <returns>A valid closed NumericalRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the maximum value is less than the minimum value.</exception>
    public static NumericalRange<T> CreateClosed(T min, T max)
    {
        return Create(min, max);
    }

    /// <summary>
    /// Creates an open-ended numerical range starting from a specific minimum value.
    /// </summary>
    /// <param name="min">The minimum value of the range.</param>
    /// <returns>A valid open-ended NumericalRange instance.</returns>
    public static NumericalRange<T> CreateFrom(T min)
    {
        return Create(min, null);
    }

    /// <summary>
    /// Creates an open-ended numerical range ending at a specific maximum value.
    /// </summary>
    /// <param name="max">The maximum value of the range.</param>
    /// <returns>A valid open-ended NumericalRange instance.</returns>
    public static NumericalRange<T> CreateUntil(T max)
    {
        return Create(null, max);
    }

    /// <summary>
    /// Creates a completely open-ended numerical range.
    /// </summary>
    /// <returns>A completely open NumericalRange instance.</returns>
    public static NumericalRange<T> CreateOpen()
    {
        return new NumericalRange<T>(null, null);
    }

    /// <summary>
    /// Attempts to create a NumericalRange instance without throwing an exception.
    /// </summary>
    /// <param name="min">The minimum value of the range (null for open-ended).</param>
    /// <param name="max">The maximum value of the range (null for open-ended).</param>
    /// <param name="result">The created NumericalRange instance if successful.</param>
    /// <returns>True if the numerical range is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(T? min, T? max, out NumericalRange<T>? result)
    {
        result = null;

        try
        {
            result = Create(min, max);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a given value falls within this numerical range (inclusive).
    /// For open-ended ranges, only checks the specified boundary.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is within the range, false otherwise.</returns>
    public bool Contains(T value)
    {
        if (Min.HasValue && value.CompareTo(Min.Value) < 0)
            return false;

        if (Max.HasValue && value.CompareTo(Max.Value) > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if this numerical range overlaps with another numerical range.
    /// </summary>
    /// <param name="other">The other numerical range to check.</param>
    /// <returns>True if the ranges overlap, false otherwise.</returns>
    public bool Overlaps(NumericalRange<T> other)
    {
        // If either range is completely open, they overlap
        if (!Min.HasValue && !Max.HasValue || !other.Min.HasValue && !other.Max.HasValue)
            return true;

        // Check for overlap considering open-ended ranges
        var thisMin = Min;
        var thisMax = Max;
        var otherMin = other.Min;
        var otherMax = other.Max;

        // No overlap if this range's max is less than other's min (when both values exist)
        if (thisMax.HasValue && otherMin.HasValue && thisMax.Value.CompareTo(otherMin.Value) < 0)
            return false;

        // No overlap if this range's min is greater than other's max (when both values exist)
        if (thisMin.HasValue && otherMax.HasValue && thisMin.Value.CompareTo(otherMax.Value) > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Indicates whether this range is open-ended (has no minimum or maximum value).
    /// </summary>
    public bool IsOpen => !Min.HasValue && !Max.HasValue;

    /// <summary>
    /// Indicates whether this range has a specified minimum value.
    /// </summary>
    public bool HasMin => Min.HasValue;

    /// <summary>
    /// Indicates whether this range has a specified maximum value.
    /// </summary>
    public bool HasMax => Max.HasValue;

    /// <summary>
    /// Indicates whether this range is closed (has both minimum and maximum values).
    /// </summary>
    public bool IsClosed => Min.HasValue && Max.HasValue;

    /// <summary>
    /// Validates that the range is closed (has both minimum and maximum values).
    /// </summary>
    /// <returns>True if the range is closed, false otherwise.</returns>
    public bool ValidateIsClosed() => IsClosed;

    /// <summary>
    /// Validates that the range has a specific boundary configuration.
    /// </summary>
    /// <param name="requireMin">True if minimum value is required.</param>
    /// <param name="requireMax">True if maximum value is required.</param>
    /// <returns>True if the range matches the required boundary configuration, false otherwise.</returns>
    public bool ValidateBoundaries(bool requireMin, bool requireMax)
    {
        return HasMin == requireMin && HasMax == requireMax;
    }

    /// <summary>
    /// Ensures that the range is closed (has both minimum and maximum values).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the range is not closed.</exception>
    public void EnsureIsClosed()
    {
        if (!IsClosed)
            throw new InvalidOperationException("Range must be closed (have both minimum and maximum values).");
    }

    /// <summary>
    /// Ensures that the range has a specific boundary configuration.
    /// </summary>
    /// <param name="requireMin">True if minimum value is required.</param>
    /// <param name="requireMax">True if maximum value is required.</param>
    /// <exception cref="InvalidOperationException">Thrown when the range doesn't match the required boundary configuration.</exception>
    public void EnsureBoundaries(bool requireMin, bool requireMax)
    {
        if (!ValidateBoundaries(requireMin, requireMax))
        {
            var minStatus = requireMin ? "required" : "not required";
            var maxStatus = requireMax ? "required" : "not required";
            throw new InvalidOperationException($"Range boundaries don't match requirements: minimum {minStatus}, maximum {maxStatus}.");
        }
    }

    /// <summary>
    /// Creates a new NumericalRange with a modified minimum value.
    /// </summary>
    /// <param name="newMin">The new minimum value (null for open-ended).</param>
    /// <returns>A new NumericalRange instance with the modified minimum value.</returns>
    public NumericalRange<T> WithMin(T? newMin) => Create(newMin, Max);

    /// <summary>
    /// Creates a new NumericalRange with a modified maximum value.
    /// </summary>
    /// <param name="newMax">The new maximum value (null for open-ended).</param>
    /// <returns>A new NumericalRange instance with the modified maximum value.</returns>
    public NumericalRange<T> WithMax(T? newMax) => Create(Min, newMax);

    /// <summary>
    /// Creates a new NumericalRange with modified minimum and maximum values.
    /// </summary>
    /// <param name="newMin">The new minimum value (null for open-ended).</param>
    /// <param name="newMax">The new maximum value (null for open-ended).</param>
    /// <returns>A new NumericalRange instance with the modified values.</returns>
    public NumericalRange<T> WithRange(T? newMin, T? newMax) => Create(newMin, newMax);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Min?.ToString() ?? "open";
        yield return Max?.ToString() ?? "open";
    }

    public override string ToString()
    {
        var min = Min?.ToString() ?? "open";
        var max = Max?.ToString() ?? "open";
        return $"{min} to {max}";
    }

    public override NumericalRange<T> Clone()
    {
        return Create(Min, Max);
    }
}

/// <summary>
/// Common numerical range implementations for convenience.
/// </summary>
public static class NumericalRange
{
    /// <summary>
    /// Creates an integer range.
    /// </summary>
    public static NumericalRange<int> CreateInt(int? min, int? max) => NumericalRange<int>.Create(min, max);

    /// <summary>
    /// Creates a decimal range.
    /// </summary>
    public static NumericalRange<decimal> CreateDecimal(decimal? min, decimal? max) => NumericalRange<decimal>.Create(min, max);

    /// <summary>
    /// Creates a double range.
    /// </summary>
    public static NumericalRange<double> CreateDouble(double? min, double? max) => NumericalRange<double>.Create(min, max);

    /// <summary>
    /// Creates a float range.
    /// </summary>
    public static NumericalRange<float> CreateFloat(float? min, float? max) => NumericalRange<float>.Create(min, max);

    /// <summary>
    /// Creates a long range.
    /// </summary>
    public static NumericalRange<long> CreateLong(long? min, long? max) => NumericalRange<long>.Create(min, max);
}
