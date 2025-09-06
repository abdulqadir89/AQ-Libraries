namespace AQ.Utilities.Results;

public enum ErrorType
{
    None,
    Validation,
    BusinessRule,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    NullValue,
    General
}

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    /// <summary>
    /// Gets the empty error instance used to represent success.
    /// </summary>
    public static readonly Error None = new(ErrorType.None, string.Empty, string.Empty);

    /// <summary>
    /// Gets the null value error.
    /// </summary>
    public static readonly Error NullValue = new(ErrorType.NullValue, "Error.NullValue", "The specified result value is null.");

    /// <summary>
    /// Creates a new validation error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new validation error.</returns>
    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);

    /// <summary>
    /// Creates a new business rule error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new business rule error.</returns>
    public static Error BusinessRule(string code, string message) => new(ErrorType.BusinessRule, code, message);

    /// <summary>
    /// Creates a new not found error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new not found error.</returns>
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    /// <summary>
    /// Creates a new conflict error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new conflict error.</returns>
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    /// <summary>
    /// Implicitly converts a string to an error with a generic code.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static implicit operator Error(string message) => new(ErrorType.General, "Error.General", message);
}
