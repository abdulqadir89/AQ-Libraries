namespace AQ.ValueObjects;

/// <summary>
/// Represents a date range value object with validation. Supports open-ended ranges.
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateOnly? Start { get; init; }
    public DateOnly? End { get; init; }

    // Parameterless constructor for EF Core
    public DateRange() { }

    private DateRange(DateOnly? start, DateOnly? end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a new DateRange instance with validation.
    /// </summary>
    /// <param name="start">The start date of the range (null for open-ended).</param>
    /// <param name="end">The end date of the range (null for open-ended).</param>
    /// <returns>A valid DateRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date is before the start date.</exception>
    public static DateRange Create(DateOnly? start, DateOnly? end)
    {
        if (start.HasValue && end.HasValue && end < start)
            throw new ArgumentException("End date cannot be before start date.", nameof(end));

        return new DateRange(start, end);
    }

    /// <summary>
    /// Creates a new DateRange instance with DateTime validation.
    /// </summary>
    /// <param name="start">The start date of the range (null for open-ended).</param>
    /// <param name="end">The end date of the range (null for open-ended).</param>
    /// <returns>A valid DateRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date is before the start date.</exception>
    public static DateRange Create(DateTime? start, DateTime? end)
    {
        DateOnly? startDateOnly = start.HasValue ? DateOnly.FromDateTime(start.Value) : null;
        DateOnly? endDateOnly = end.HasValue ? DateOnly.FromDateTime(end.Value) : null;
        return Create(startDateOnly, endDateOnly);
    }

    /// <summary>
    /// Creates a closed date range with both start and end dates.
    /// </summary>
    /// <param name="start">The start date of the range.</param>
    /// <param name="end">The end date of the range.</param>
    /// <returns>A valid closed DateRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date is before the start date.</exception>
    public static DateRange CreateClosed(DateOnly start, DateOnly end)
    {
        return Create(start, end);
    }

    /// <summary>
    /// Creates an open-ended date range starting from a specific date.
    /// </summary>
    /// <param name="start">The start date of the range.</param>
    /// <returns>A valid open-ended DateRange instance.</returns>
    public static DateRange CreateFrom(DateOnly start)
    {
        return Create(start, null);
    }

    /// <summary>
    /// Creates an open-ended date range ending at a specific date.
    /// </summary>
    /// <param name="end">The end date of the range.</param>
    /// <returns>A valid open-ended DateRange instance.</returns>
    public static DateRange CreateUntil(DateOnly end)
    {
        return Create(null, end);
    }

    /// <summary>
    /// Creates a completely open-ended date range.
    /// </summary>
    /// <returns>A completely open DateRange instance.</returns>
    public static DateRange CreateOpen()
    {
        return new DateRange(null, null);
    }

    /// <summary>
    /// Attempts to create a DateRange instance without throwing an exception.
    /// </summary>
    /// <param name="start">The start date of the range (null for open-ended).</param>
    /// <param name="end">The end date of the range (null for open-ended).</param>
    /// <param name="result">The created DateRange instance if successful.</param>
    /// <returns>True if the date range is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(DateOnly? start, DateOnly? end, out DateRange? result)
    {
        result = null;

        try
        {
            result = Create(start, end);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to create a DateRange instance from DateTime without throwing an exception.
    /// </summary>
    /// <param name="start">The start date of the range (null for open-ended).</param>
    /// <param name="end">The end date of the range (null for open-ended).</param>
    /// <param name="result">The created DateRange instance if successful.</param>
    /// <returns>True if the date range is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(DateTime? start, DateTime? end, out DateRange? result)
    {
        DateOnly? startDateOnly = start.HasValue ? DateOnly.FromDateTime(start.Value) : null;
        DateOnly? endDateOnly = end.HasValue ? DateOnly.FromDateTime(end.Value) : null;
        return TryCreate(startDateOnly, endDateOnly, out result);
    }

    /// <summary>
    /// Gets the duration of the date range in days.
    /// Returns null if either start or end date is not specified.
    /// </summary>
    /// <returns>The number of days in the range (inclusive) or null if range is open-ended.</returns>
    public int? GetDurationInDays()
    {
        if (!Start.HasValue || !End.HasValue)
            return null;

        return End.Value.DayNumber - Start.Value.DayNumber + 1;
    }

    /// <summary>
    /// Checks if a given date falls within this date range (inclusive).
    /// For open-ended ranges, only checks the specified boundary.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is within the range, false otherwise.</returns>
    public bool Contains(DateOnly date)
    {
        if (Start.HasValue && date < Start.Value)
            return false;

        if (End.HasValue && date > End.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a given DateTime falls within this date range (inclusive).
    /// </summary>
    /// <param name="date">The DateTime to check.</param>
    /// <returns>True if the date is within the range, false otherwise.</returns>
    public bool Contains(DateTime date) => Contains(DateOnly.FromDateTime(date));

    /// <summary>
    /// Checks if this date range overlaps with another date range.
    /// </summary>
    /// <param name="other">The other date range to check.</param>
    /// <returns>True if the ranges overlap, false otherwise.</returns>
    public bool Overlaps(DateRange other)
    {
        // If either range is completely open, they overlap
        if (!Start.HasValue && !End.HasValue || !other.Start.HasValue && !other.End.HasValue)
            return true;

        // Check for overlap considering open-ended ranges
        var thisStart = Start ?? DateOnly.MinValue;
        var thisEnd = End ?? DateOnly.MaxValue;
        var otherStart = other.Start ?? DateOnly.MinValue;
        var otherEnd = other.End ?? DateOnly.MaxValue;

        return thisStart <= otherEnd && thisEnd >= otherStart;
    }

    /// <summary>
    /// Indicates whether this range is open-ended (has no start or end date).
    /// </summary>
    public bool GetIsOpen() => !Start.HasValue && !End.HasValue;

    /// <summary>
    /// Indicates whether this range has a specified start date.
    /// </summary>
    public bool HasStart() => Start.HasValue;

    /// <summary>
    /// Indicates whether this range has a specified end date.
    /// </summary>
    public bool HasEnd() => End.HasValue;

    /// <summary>
    /// Indicates whether this range is closed (has both start and end dates).
    /// </summary>
    public bool IsClosed() => Start.HasValue && End.HasValue;

    /// <summary>
    /// Validates that the range is closed (has both start and end dates).
    /// </summary>
    /// <returns>True if the range is closed, false otherwise.</returns>
    public bool ValidateIsClosed() => IsClosed();

    /// <summary>
    /// Validates that the range has a specific boundary configuration.
    /// </summary>
    /// <param name="requireStart">True if start date is required.</param>
    /// <param name="requireEnd">True if end date is required.</param>
    /// <returns>True if the range matches the required boundary configuration, false otherwise.</returns>
    public bool ValidateBoundaries(bool requireStart, bool requireEnd)
    {
        return HasStart() == requireStart && HasEnd() == requireEnd;
    }

    /// <summary>
    /// Ensures that the range is closed (has both start and end dates).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the range is not closed.</exception>
    public void EnsureIsClosed()
    {
        if (!IsClosed())
            throw new InvalidOperationException("Range must be closed (have both start and end dates).");
    }

    /// <summary>
    /// Ensures that the range has a specific boundary configuration.
    /// </summary>
    /// <param name="requireStart">True if start date is required.</param>
    /// <param name="requireEnd">True if end date is required.</param>
    /// <exception cref="InvalidOperationException">Thrown when the range doesn't match the required boundary configuration.</exception>
    public void EnsureBoundaries(bool requireStart, bool requireEnd)
    {
        if (!ValidateBoundaries(requireStart, requireEnd))
        {
            var startStatus = requireStart ? "required" : "not required";
            var endStatus = requireEnd ? "required" : "not required";
            throw new InvalidOperationException($"Range boundaries don't match requirements: start date {startStatus}, end date {endStatus}.");
        }
    }

    /// <summary>
    /// Creates a new DateRange with a modified start date.
    /// </summary>
    /// <param name="newStartDate">The new start date (null for open-ended).</param>
    /// <returns>A new DateRange instance with the modified start date.</returns>
    public DateRange WithStartDate(DateOnly? newStartDate) => Create(newStartDate, End);

    /// <summary>
    /// Creates a new DateRange with a modified end date.
    /// </summary>
    /// <param name="newEndDate">The new end date (null for open-ended).</param>
    /// <returns>A new DateRange instance with the modified end date.</returns>
    public DateRange WithEndDate(DateOnly? newEndDate) => Create(Start, newEndDate);

    /// <summary>
    /// Creates a new DateRange with modified start and end dates.
    /// </summary>
    /// <param name="newStartDate">The new start date (null for open-ended).</param>
    /// <param name="newEndDate">The new end date (null for open-ended).</param>
    /// <returns>A new DateRange instance with the modified dates.</returns>
    public DateRange WithDates(DateOnly? newStartDate, DateOnly? newEndDate) => Create(newStartDate, newEndDate);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Start ?? (object)"open";
        yield return End ?? (object)"open";
    }

    public override string ToString()
    {
        var start = Start?.ToString("yyyy-MM-dd") ?? "open";
        var end = End?.ToString("yyyy-MM-dd") ?? "open";
        return $"{start} to {end}";
    }

    public override DateRange Clone()
    {
        return Create(Start, End);
    }
}
