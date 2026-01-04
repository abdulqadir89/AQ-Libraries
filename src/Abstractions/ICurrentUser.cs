namespace AQ.Abstractions;

/// <summary>
/// Service to get information about the current authenticated user
/// Not to be used in endpoints directly
/// </summary>
public interface ICurrentUserService
{
    string? GetCurrentUserId();
    bool IsAuthenticated { get; }
}
