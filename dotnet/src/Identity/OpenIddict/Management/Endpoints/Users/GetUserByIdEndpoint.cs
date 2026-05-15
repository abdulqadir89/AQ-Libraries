using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Management.Dto;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class GetUserByIdRequest
{
    public Guid Id { get; set; }
}

public class GetUserByIdEndpoint(
    IIdentityDbContext context,
    UserManager<AQ.Identity.Core.Entities.ApplicationUser> userManager,
    IOpenIddictTokenManager tokenManager)
    : Endpoint<GetUserByIdRequest, UserDetailDto>
{
    public override void Configure()
    {
        Get("/manage/users/{Id}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get MFA status
        var mfaEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        // Count active tokens
        var activeTokenCount = 0;
        var tokens = tokenManager.FindBySubjectAsync(user.Id.ToString(), ct);
        await foreach (var token in tokens)
        {
            var status = await tokenManager.GetStatusAsync(token, ct);
            if (status == Statuses.Valid)
            {
                activeTokenCount++;
            }
        }

        var response = new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsActive = user.IsActive,
            MfaEnabled = mfaEnabled,
            ActiveTokenCount = activeTokenCount,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        await Send.OkAsync(response, ct);
    }
}
