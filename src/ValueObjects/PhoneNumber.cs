using System.Text.RegularExpressions;

namespace AQ.ValueObjects;

/// <summary>
/// Represents a phone number value object with validation.
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex PhonePattern = new(
        @"^(\+?[1-9]\d{1,14})$",
        RegexOptions.Compiled);

    public string Value { get; init; }

    // Parameterless constructor for EF Core
    public PhoneNumber()
    {
        Value = default!;
    }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new PhoneNumber instance with validation.
    /// </summary>
    /// <param name="phoneNumber">The phone number string.</param>
    /// <returns>A valid PhoneNumber instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the phone number format is invalid.</exception>
    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

        var normalizedPhone = phoneNumber.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (!PhonePattern.IsMatch(normalizedPhone))
            throw new ArgumentException($"Invalid phone number format: {phoneNumber}", nameof(phoneNumber));

        return new PhoneNumber(normalizedPhone);
    }

    /// <summary>
    /// Attempts to create a PhoneNumber instance without throwing an exception.
    /// </summary>
    /// <param name="phoneNumber">The phone number string.</param>
    /// <param name="result">The created PhoneNumber instance if successful.</param>
    /// <returns>True if the phone number is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(string phoneNumber, out PhoneNumber? result)
    {
        result = null;

        try
        {
            result = Create(phoneNumber);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new PhoneNumber with a modified value.
    /// </summary>
    /// <param name="newPhoneNumber">The new phone number string.</param>
    /// <returns>A new PhoneNumber instance with the modified value.</returns>
    public PhoneNumber WithValue(string newPhoneNumber) => Create(newPhoneNumber);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
}
