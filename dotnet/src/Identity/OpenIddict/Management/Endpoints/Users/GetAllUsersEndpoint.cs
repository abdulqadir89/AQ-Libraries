using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Management.Dto;
using AQ.Utilities.Results;
using AQ.Utilities.Results.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class GetAllUsersRequest : BaseManageRequest
{
    [QueryParam] public string? SearchEmail { get; set; }
}

public class GetAllUsersEndpoint(IIdentityDbContext context) : Endpoint<GetAllUsersRequest, DataSet<UserSummaryDto>>
{
    public override void Configure()
    {
        Get("/manage/users");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(GetAllUsersRequest req, CancellationToken ct)
    {
        var query = context.Users.AsNoTracking();

        // Search by email or fullname
        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
        {
            var searchLower = req.SearchTerm.ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(searchLower) ||
                                     u.FullName.ToLower().Contains(searchLower));
        }

        // Additional email-specific search
        if (!string.IsNullOrWhiteSpace(req.SearchEmail))
        {
            var emailLower = req.SearchEmail.ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(emailLower));
        }

        // Project to DTO
        var dtoQuery = query.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            FullName = u.FullName,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        });

        // Default sort by FullName
        dtoQuery = dtoQuery.OrderBy(u => u.FullName);

        var result = await dtoQuery.ToDataResultAsync(
            req.Skip,
            req.Take,
            (q, c) => q.CountAsync(c),
            (q, c) => q.ToListAsync(c),
            ct);

        await Send.OkAsync(result, ct);
    }
}
