namespace AQ.Common.Application.Services;

public interface ICurrentUserService
{
    Guid? GetCurrentUserId();
    bool IsAuthenticated { get; }
}
