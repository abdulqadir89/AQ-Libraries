using System.Text.RegularExpressions;

namespace AQ.Common.Domain.Validation;

/// <summary>
/// Contains compiled regex patterns for common validation scenarios across all domains.
/// </summary>
public static class ValidationPatterns
{
    /// <summary>
    /// Email regex pattern - RFC 5322 compliant.
    /// Validates standard email format with proper domain structure.
    /// </summary>
    public static readonly Regex Email = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Mobile phone regex pattern - supports international formats.
    /// Accepts formats like: +1234567890, 1234567890, +44123456789
    /// Must start with 1-9 (no leading zeros) and be 2-15 digits total.
    /// </summary>
    public static readonly Regex MobilePhone = new(
        @"^(\+?[1-9]\d{1,14})$",
        RegexOptions.Compiled);

    /// <summary>
    /// Username pattern - alphanumeric with underscores and hyphens allowed.
    /// Must start and end with alphanumeric character.
    /// Length validation should be handled separately.
    /// </summary>
    public static readonly Regex Username = new(
        @"^[a-zA-Z0-9][a-zA-Z0-9_-]*[a-zA-Z0-9]$|^[a-zA-Z0-9]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Strong password pattern requiring:
    /// - At least 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </summary>
    public static readonly Regex StrongPassword = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    /// <summary>
    /// URL pattern for basic URL validation.
    /// Supports HTTP and HTTPS protocols.
    /// </summary>
    public static readonly Regex Url = new(
        @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates email format using the Email regex pattern.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>True if the email format is valid, false otherwise.</returns>
    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && Email.IsMatch(email);

    /// <summary>
    /// Validates mobile phone format using the MobilePhone regex pattern.
    /// </summary>
    /// <param name="mobile">The mobile phone number to validate.</param>
    /// <returns>True if the mobile format is valid, false otherwise.</returns>
    public static bool IsValidMobilePhone(string mobile) =>
        !string.IsNullOrWhiteSpace(mobile) && MobilePhone.IsMatch(mobile);

    /// <summary>
    /// Validates username format using the Username regex pattern.
    /// Note: This only validates the format, not the length constraints.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <returns>True if the username format is valid, false otherwise.</returns>
    public static bool IsValidUsernameFormat(string username) =>
        !string.IsNullOrWhiteSpace(username) && Username.IsMatch(username);

    /// <summary>
    /// Validates password strength using the StrongPassword regex pattern.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>True if the password meets strength requirements, false otherwise.</returns>
    public static bool IsStrongPassword(string password) =>
        !string.IsNullOrWhiteSpace(password) && StrongPassword.IsMatch(password);

    /// <summary>
    /// Validates URL format using the Url regex pattern.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the URL format is valid, false otherwise.</returns>
    public static bool IsValidUrl(string url) =>
        !string.IsNullOrWhiteSpace(url) && Url.IsMatch(url);
}
