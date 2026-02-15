using System.ComponentModel.DataAnnotations;

namespace AQ.ValueObjects;

/// <summary>
/// Validates that a date range is closed (has both start and end values).
/// Supports DateRange and DateTimeOffsetRange.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ClosedRangeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var memberName = validationContext.MemberName ?? string.Empty;

        if (value is DateTimeOffsetRange dateTimeOffsetRange && !dateTimeOffsetRange.IsClosed())
        {
            return new ValidationResult(
                ErrorMessage ?? "Range must be closed (have both start and end date times).",
                [memberName]);
        }

        if (value is DateRange dateRange && !dateRange.IsClosed())
        {
            return new ValidationResult(
                ErrorMessage ?? "Range must be closed (have both start and end dates).",
                [memberName]);
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validates that a date range has specific boundary configuration.
/// Supports DateRange and DateTimeOffsetRange.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RangeBoundariesAttribute : ValidationAttribute
{
    public bool RequireStart { get; set; }
    public bool RequireEnd { get; set; }

    public RangeBoundariesAttribute(bool requireStart, bool requireEnd)
    {
        RequireStart = requireStart;
        RequireEnd = requireEnd;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var memberName = validationContext.MemberName ?? string.Empty;
        var startStatus = RequireStart ? "required" : "optional";
        var endStatus = RequireEnd ? "required" : "optional";

        if (value is DateTimeOffsetRange dateTimeOffsetRange && !dateTimeOffsetRange.ValidateBoundaries(RequireStart, RequireEnd))
        {
            return new ValidationResult(
                ErrorMessage ?? $"Range boundaries invalid: start is {startStatus}, end is {endStatus}.",
                [memberName]);
        }

        if (value is DateRange dateRange && !dateRange.ValidateBoundaries(RequireStart, RequireEnd))
        {
            return new ValidationResult(
                ErrorMessage ?? $"Range boundaries invalid: start is {startStatus}, end is {endStatus}.",
                [memberName]);
        }

        return ValidationResult.Success!;
    }
}
