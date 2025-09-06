using AQ.ValueObjects;
using System.Text.RegularExpressions;

namespace AQ.ValueObjects;

/// <summary>
/// Represents an email address value object with validation.
/// </summary>
public sealed partial class Email : ValueObject
{
    private static readonly Regex EmailPattern = MyRegex();

    public string Value { get; init; }

    // Parameterless constructor for EF Core
    private Email()
    {
        Value = default!;
    }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Email instance with validation.
    /// </summary>
    /// <param name="email">The email address string.</param>
    /// <returns>A valid Email instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the email format is invalid.</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > 254)
            throw new ArgumentException("Email address cannot exceed 254 characters.", nameof(email));

        if (!EmailPattern.IsMatch(normalizedEmail))
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));

        return new Email(normalizedEmail);
    }

    /// <summary>
    /// Attempts to create an Email instance without throwing an exception.
    /// </summary>
    /// <param name="email">The email address string.</param>
    /// <param name="result">The created Email instance if successful.</param>
    /// <returns>True if the email is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(string email, out Email? result)
    {
        result = null;

        try
        {
            result = Create(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new Email with a modified value.
    /// </summary>
    /// <param name="newEmail">The new email address string.</param>
    /// <returns>A new Email instance with the modified value.</returns>
    public Email WithValue(string newEmail) => Create(newEmail);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-PK")]
    private static partial Regex MyRegex();
}
