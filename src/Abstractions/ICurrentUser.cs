namespace AQ.Abstractions;

public interface ICurrentUserService
{
    string? GetCurrentUserId();
    bool IsAuthenticated { get; }
}
