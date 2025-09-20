namespace AQ.ValueObjects;

/// <summary>
/// Represents a full name value object with validation.
/// </summary>
public sealed class FullName : ValueObject
{
    public string Value { get; init; }

    // Parameterless constructor for EF Core
    public FullName()
    {
        Value = default!;
    }

    private FullName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new FullName instance with validation.
    /// </summary>
    /// <param name="fullName">The full name string.</param>
    /// <returns>A valid FullName instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the full name is invalid.</exception>
    public static FullName Create(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty.", nameof(fullName));

        var trimmedName = fullName.Trim();

        if (trimmedName.Length > 100)
            throw new ArgumentException("Full name cannot exceed 100 characters.", nameof(fullName));

        if (trimmedName.Length < 2)
            throw new ArgumentException("Full name must be at least 2 characters long.", nameof(fullName));

        return new FullName(trimmedName);
    }

    /// <summary>
    /// Attempts to create a FullName instance without throwing an exception.
    /// </summary>
    /// <param name="fullName">The full name string.</param>
    /// <param name="result">The created FullName instance if successful.</param>
    /// <returns>True if the full name is valid and created successfully, false otherwise.</returns>
    public static bool TryCreate(string fullName, out FullName? result)
    {
        result = null;

        try
        {
            result = Create(fullName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new FullName with a modified value.
    /// </summary>
    /// <param name="newFullName">The new full name string.</param>
    /// <returns>A new FullName instance with the modified value.</returns>
    public FullName WithValue(string newFullName) => Create(newFullName);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(FullName fullName) => fullName.Value;

    public override FullName Clone()
    {
        return Create(Value);
    }
}
