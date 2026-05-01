using AQ.Identity.Core.Abstractions;
using AQ.Identity.OpenIddict.Management.Dto;
using AQ.Utilities.Results;
using AQ.Utilities.Results.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.AuditLog;

public class GetAuditLogRequest : BaseManageRequest
{
    [QueryParam] public Guid? UserId { get; set; }
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
    }

    public override async Task HandleAsync(GetAuditLogRequest req, CancellationToken ct)
    {
        var query = context.AuditLog.AsNoTracking();

        if (req.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == req.UserId.Value);
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
            UserId = a.UserId,
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
