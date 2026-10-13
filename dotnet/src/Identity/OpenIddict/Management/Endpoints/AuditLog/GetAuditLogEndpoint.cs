using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Management.Dto;
using AQ.Utilities.Results;
using AQ.Utilities.Results.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.AuditLog;

public class GetAuditLogRequest : BaseManageRequest
{
    /// <summary>Filters to actions applied to this user.</summary>
    [QueryParam] public Guid? SubjectUserId { get; set; }
    /// <summary>Filters to actions performed by this principal.</summary>
    [QueryParam] public Guid? ActorId { get; set; }
    [QueryParam] public string? Action { get; set; }
    [QueryParam] public DateTimeOffset? From { get; set; }
    [QueryParam] public DateTimeOffset? To { get; set; }
}

public class GetAuditLogEndpoint(IIdentityDbContext context)
    : Endpoint<GetAuditLogRequest, DataSet<AuditEntryDto>>
{
    public override void Configure()
    {
        Get("/manage/audit-log");
        Policies("ManageApi");
        // Machine API: bearer tokens (OpenIddict validation), never the browser cookie
        AuthSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    }

    public override async Task HandleAsync(GetAuditLogRequest req, CancellationToken ct)
    {
        var query = context.AuditLog.AsNoTracking();

        if (req.SubjectUserId.HasValue)
        {
            query = query.Where(a => a.SubjectUserId == req.SubjectUserId.Value);
        }

        if (req.ActorId.HasValue)
        {
            query = query.Where(a => a.ActorId == req.ActorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Action))
        {
            query = query.Where(a => a.Action == req.Action);
        }

        if (req.From.HasValue)
        {
            query = query.Where(a => a.OccurredAt >= req.From.Value);
        }

        if (req.To.HasValue)
        {
            query = query.Where(a => a.OccurredAt <= req.To.Value);
        }

        var dtoQuery = query.Select(a => new AuditEntryDto
        {
            Id = a.Id,
            ActorKind = a.ActorKind,
            ActorId = a.ActorId,
            SubjectUserId = a.SubjectUserId,
            ClientId = a.ClientId,
            Action = a.Action,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent,
            OccurredAt = a.OccurredAt
        });

        dtoQuery = dtoQuery.OrderByDescending(a => a.OccurredAt);

        var result = await dtoQuery.ToDataResultAsync(
            req.Skip,
            req.Take,
            (q, c) => q.CountAsync(c),
            (q, c) => q.ToListAsync(c),
            ct);

        await Send.OkAsync(result, ct);
    }
}
