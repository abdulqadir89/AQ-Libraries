using AQ.Common.Application.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AQ.Common.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
