using System.Linq.Expressions;

namespace AQ.ValueObjects;

/// <summary>
/// DTO for DateTimeOffsetRange value object serialization in API endpoints.
/// </summary>
public class DateTimeOffsetRangeDto
{
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
}

/// <summary>
/// Represents a date time range value object with validation and timezone support. Supports open-ended ranges.
/// </summary>
public sealed class DateTimeOffsetRange : ValueObject
{
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }

    // Parameterless constructor for EF Core
    public DateTimeOffsetRange() { }

    private DateTimeOffsetRange(DateTimeOffset? start, DateTimeOffset? end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a new DateTimeRange instance with validation.
    /// </summary>
    /// <param name="start">The start date time of the range (null for open-ended).</param>
    /// <param name="end">The end date time of the range (null for open-ended).</param>
    /// <returns>A valid DateTimeRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date time is before the start date time.</exception>
    public static DateTimeOffsetRange Create(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue && end.HasValue && end < start)
            throw new ArgumentException("End date time cannot be before start date time.", nameof(end));

        return new DateTimeOffsetRange(start, end);
    }

    /// <summary>
    /// Creates a new DateTimeRange instance with DateTime validation (converted to DateTimeOffset with system timezone).
    /// </summary>
    /// <param name="start">The start date time of the range (null for open-ended).</param>
    /// <param name="end">The end date time of the range (null for open-ended).</param>
    /// <returns>A valid DateTimeRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date time is before the start date time.</exception>
    public static DateTimeOffsetRange Create(DateTime? start, DateTime? end)
    {
        DateTimeOffset? startDateTimeOffset = start.HasValue ? new DateTimeOffset(start.Value) : null;
        DateTimeOffset? endDateTimeOffset = end.HasValue ? new DateTimeOffset(end.Value) : null;
        return Create(startDateTimeOffset, endDateTimeOffset);
    }

    /// <summary>
    /// Creates a new DateTimeRange instance with timezone specification.
    /// </summary>
    /// <param name="start">The start date time of the range (null for open-ended).</param>
    /// <param name="end">The end date time of the range (null for open-ended).</param>
    /// <param name="offset">The timezone offset to apply to both dates.</param>
    /// <returns>A valid DateTimeRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date time is before the start date time.</exception>
    public static DateTimeOffsetRange Create(DateTime? start, DateTime? end, TimeSpan offset)
    {
        DateTimeOffset? startDateTimeOffset = start.HasValue ? new DateTimeOffset(start.Value, offset) : null;
        DateTimeOffset? endDateTimeOffset = end.HasValue ? new DateTimeOffset(end.Value, offset) : null;
        return Create(startDateTimeOffset, endDateTimeOffset);
    }

    /// <summary>
    /// Creates a closed date time range with both start and end date times.
    /// </summary>
    /// <param name="start">The start date time of the range.</param>
    /// <param name="end">The end date time of the range.</param>
    /// <returns>A valid closed DateTimeRange instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the end date time is before the start date time.</exception>
    public static DateTimeOffsetRange CreateClosed(DateTimeOffset start, DateTimeOffset end)
    {
        return Create(start, end);
    }

    /// <summary>
    /// Creates an open-ended date time range starting from a specific date time.
    /// </summary>
    /// <param name="start">The start date time of the range.</param>
    /// <returns>A valid open-ended DateTimeRange instance.</returns>
    public static DateTimeOffsetRange CreateFrom(DateTimeOffset start)
    {
        return Create(start, null);
    }

    /// <summary>
    /// Creates an open-ended date time range ending at a specific date time.
    /// </summary>
    /// <param name="end">The end date time of the range.</param>
    /// <returns>A valid open-ended DateTimeRange instance.</returns>
    public static DateTimeOffsetRange CreateUntil(DateTimeOffset end)
    {
        return Create(null, end);
    }

    /// <summary>
    /// Creates a completely open-ended date time range.
    /// </summary>
    /// <returns>A completely open DateTimeRange instance.</returns>
    public static DateTimeOffsetRange CreateOpen()
    {
        return new DateTimeOffsetRange(null, null);
    }

    /// <summary>
    /// Attempts to create a DateTimeRange instance without throwing an exception.
    /// </summary>
    /// <param name="start">The start date time of the range (null for open-ended).</param>
    /// <param name="end">The end date time of the range (null for open-ended).</param>
    /// <param name="result">The created DateTimeRange instance if successful.</param>
    /// <returns>True if the date time range is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(DateTimeOffset? start, DateTimeOffset? end, out DateTimeOffsetRange? result)
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
    /// Attempts to create a DateTimeRange instance from DateTime without throwing an exception.
    /// </summary>
    /// <param name="start">The start date time of the range (null for open-ended).</param>
    /// <param name="end">The end date time of the range (null for open-ended).</param>
    /// <param name="result">The created DateTimeRange instance if successful.</param>
    /// <returns>True if the date time range is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(DateTime? start, DateTime? end, out DateTimeOffsetRange? result)
    {
        DateTimeOffset? startDateTimeOffset = start.HasValue ? new DateTimeOffset(start.Value) : null;
        DateTimeOffset? endDateTimeOffset = end.HasValue ? new DateTimeOffset(end.Value) : null;
        return TryCreate(startDateTimeOffset, endDateTimeOffset, out result);
    }

    /// <summary>
    /// Attempts to create a DateTimeRange instance with timezone specification without throwing an exception.
    /// </summary>
    /// <param name="start">The start date time of the range (null for open-ended).</param>
    /// <param name="end">The end date time of the range (null for open-ended).</param>
    /// <param name="offset">The timezone offset to apply to both dates.</param>
    /// <param name="result">The created DateTimeRange instance if successful.</param>
    /// <returns>True if the date time range is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(DateTime? start, DateTime? end, TimeSpan offset, out DateTimeOffsetRange? result)
    {
        DateTimeOffset? startDateTimeOffset = start.HasValue ? new DateTimeOffset(start.Value, offset) : null;
        DateTimeOffset? endDateTimeOffset = end.HasValue ? new DateTimeOffset(end.Value, offset) : null;
        return TryCreate(startDateTimeOffset, endDateTimeOffset, out result);
    }

    /// <summary>
    /// Gets the duration of the date time range.
    /// Returns null if either start or end date time is not specified.
    /// </summary>
    /// <returns>The duration of the range or null if range is open-ended.</returns>
    public TimeSpan? GetDuration()
    {
        if (!Start.HasValue || !End.HasValue)
            return null;

        return End.Value - Start.Value;
    }

    /// <summary>
    /// Gets the duration of the date time range in total days.
    /// Returns null if either start or end date time is not specified.
    /// </summary>
    /// <returns>The number of days in the range or null if range is open-ended.</returns>
    public double? GetDurationInDays()
    {
        var duration = GetDuration();
        return duration?.TotalDays;
    }

    /// <summary>
    /// Gets the duration of the date time range in total hours.
    /// Returns null if either start or end date time is not specified.
    /// </summary>
    /// <returns>The number of hours in the range or null if range is open-ended.</returns>
    public double? GetDurationInHours()
    {
        var duration = GetDuration();
        return duration?.TotalHours;
    }

    /// <summary>
    /// Gets the duration of the date time range in total minutes.
    /// Returns null if either start or end date time is not specified.
    /// </summary>
    /// <returns>The number of minutes in the range or null if range is open-ended.</returns>
    public double? GetDurationInMinutes()
    {
        var duration = GetDuration();
        return duration?.TotalMinutes;
    }

    /// <summary>
    /// Checks if a given date time falls within this date time range (inclusive).
    /// For open-ended ranges, only checks the specified boundary.
    /// </summary>
    /// <param name="dateTime">The date time to check.</param>
    /// <returns>True if the date time is within the range, false otherwise.</returns>
    public bool Contains(DateTimeOffset dateTime)
    {
        if (Start.HasValue && dateTime < Start.Value)
            return false;

        if (End.HasValue && dateTime > End.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a given DateTime falls within this date time range (inclusive).
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the date time is within the range, false otherwise.</returns>
    public bool Contains(DateTime dateTime) => Contains(new DateTimeOffset(dateTime));


    /// <summary>
    /// Gets an expression that can be used in LINQ queries to check if a given date time falls within this date time range (inclusive).
    /// </summary>
    public Expression<Func<DateTimeOffset, bool>> ContainsExpression =>
        dateTime => (!Start.HasValue || dateTime >= Start.Value) && (!End.HasValue || dateTime <= End.Value);

    /// <summary>
    /// Checks if this date time range overlaps with another date time range.
    /// </summary>
    /// <param name="other">The other date time range to check.</param>
    /// <returns>True if the ranges overlap, false otherwise.</returns>
    public bool Overlaps(DateTimeOffsetRange other)
    {
        // If either range is completely open, they overlap
        if (!Start.HasValue && !End.HasValue || !other.Start.HasValue && !other.End.HasValue)
            return true;

        // Check for overlap considering open-ended ranges
        var thisStart = Start ?? DateTimeOffset.MinValue;
        var thisEnd = End ?? DateTimeOffset.MaxValue;
        var otherStart = other.Start ?? DateTimeOffset.MinValue;
        var otherEnd = other.End ?? DateTimeOffset.MaxValue;

        return thisStart <= otherEnd && thisEnd >= otherStart;
    }

    /// <summary>
    /// Converts the DateTimeRange to UTC timezone.
    /// </summary>
    /// <returns>A new DateTimeRange with UTC timezone.</returns>
    public DateTimeOffsetRange ToUtc()
    {
        var startUtc = Start?.ToUniversalTime();
        var endUtc = End?.ToUniversalTime();
        return new DateTimeOffsetRange(startUtc, endUtc);
    }

    /// <summary>
    /// Converts the DateTimeRange to a specific timezone.
    /// </summary>
    /// <param name="offset">The target timezone offset.</param>
    /// <returns>A new DateTimeRange with the specified timezone.</returns>
    public DateTimeOffsetRange ToOffset(TimeSpan offset)
    {
        var startOffset = Start?.ToOffset(offset);
        var endOffset = End?.ToOffset(offset);
        return new DateTimeOffsetRange(startOffset, endOffset);
    }

    /// <summary>
    /// Indicates whether this range is open-ended (has no start or end date time).
    /// </summary>
    public bool IsOpen() => !Start.HasValue && !End.HasValue;

    /// <summary>
    /// Indicates whether this range has a specified start date time.
    /// </summary>
    public bool HasStart() => Start.HasValue;

    /// <summary>
    /// Indicates whether this range has a specified end date time.
    /// </summary>
    public bool HasEnd() => End.HasValue;

    /// <summary>
    /// Indicates whether this range is closed (has both start and end date times).
    /// </summary>
    public bool IsClosed() => Start.HasValue && End.HasValue;

    /// <summary>
    /// Validates that the range is closed (has both start and end date times).
    /// </summary>
    /// <returns>True if the range is closed, false otherwise.</returns>
    public bool ValidateIsClosed() => IsClosed();

    /// <summary>
    /// Validates that the range has a specific boundary configuration.
    /// </summary>
    /// <param name="requireStart">True if start date time is required.</param>
    /// <param name="requireEnd">True if end date time is required.</param>
    /// <returns>True if the range matches the required boundary configuration, false otherwise.</returns>
    public bool ValidateBoundaries(bool requireStart, bool requireEnd)
    {
        return HasStart() == requireStart && HasEnd() == requireEnd;
    }

    /// <summary>
    /// Ensures that the range is closed (has both start and end date times).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the range is not closed.</exception>
    public void EnsureIsClosed()
    {
        if (!IsClosed())
            throw new InvalidOperationException("Range must be closed (have both start and end date times).");
    }

    /// <summary>
    /// Ensures that the range has a specific boundary configuration.
    /// </summary>
    /// <param name="requireStart">True if start date time is required.</param>
    /// <param name="requireEnd">True if end date time is required.</param>
    /// <exception cref="InvalidOperationException">Thrown when the range doesn't match the required boundary configuration.</exception>
    public void EnsureBoundaries(bool requireStart, bool requireEnd)
    {
        if (!ValidateBoundaries(requireStart, requireEnd))
        {
            var startStatus = requireStart ? "required" : "not required";
            var endStatus = requireEnd ? "required" : "not required";
            throw new InvalidOperationException($"Range boundaries don't match requirements: start date time {startStatus}, end date time {endStatus}.");
        }
    }

    /// <summary>
    /// Creates a new DateTimeRange with a modified start date time.
    /// </summary>
    /// <param name="newStart">The new start date time (null for open-ended).</param>
    /// <returns>A new DateTimeRange instance with the modified start date time.</returns>
    public DateTimeOffsetRange WithStart(DateTimeOffset? newStart) => Create(newStart, End);

    /// <summary>
    /// Creates a new DateTimeRange with a modified end date time.
    /// </summary>
    /// <param name="newEnd">The new end date time (null for open-ended).</param>
    /// <returns>A new DateTimeRange instance with the modified end date time.</returns>
    public DateTimeOffsetRange WithEnd(DateTimeOffset? newEnd) => Create(Start, newEnd);

    /// <summary>
    /// Creates a new DateTimeRange with modified start and end date times.
    /// </summary>
    /// <param name="newStart">The new start date time (null for open-ended).</param>
    /// <param name="newEnd">The new end date time (null for open-ended).</param>
    /// <returns>A new DateTimeRange instance with the modified date times.</returns>
    public DateTimeOffsetRange WithDateTimes(DateTimeOffset? newStart, DateTimeOffset? newEnd) => Create(newStart, newEnd);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Start ?? (object)"open";
        yield return End ?? (object)"open";
    }

    public override string ToString()
    {
        var start = Start?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "open";
        var end = End?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "open";
        return $"{start} to {end}";
    }

    public override DateTimeOffsetRange Clone()
    {
        return Create(Start, End);
    }

    /// <summary>
    /// Converts this DateTimeOffsetRange to a DTO for API serialization.
    /// </summary>
    public DateTimeOffsetRangeDto ToDto()
    {
        return new DateTimeOffsetRangeDto
        {
            Start = Start,
            End = End
        };
    }

    /// <summary>
    /// Creates a DateTimeOffsetRange from a DTO.
    /// </summary>
    public static DateTimeOffsetRange FromDto(DateTimeOffsetRangeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Create(dto.Start, dto.End);
    }

    /// <summary>
    /// Attempts to create a DateTimeOffsetRange from a DTO without throwing exceptions.
    /// </summary>
    public static bool TryFromDto(DateTimeOffsetRangeDto? dto, out DateTimeOffsetRange? result)
    {
        result = null;
        if (dto == null) return false;

        return TryCreate(dto.Start, dto.End, out result);
    }
}
