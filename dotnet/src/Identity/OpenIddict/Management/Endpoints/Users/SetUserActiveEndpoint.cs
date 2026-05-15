using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class SetUserActiveRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
}

public class SetUserActiveEndpoint(
    IIdentityDbContext context,
    UserManager<ApplicationUser> userManager)
    : Endpoint<SetUserActiveRequest>
{
    public override void Configure()
    {
        Put("/manage/users/{Id}/active");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(SetUserActiveRequest req, CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var previousState = user.IsActive;
        user.IsActive = req.IsActive;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ThrowError("Failed to update user status");
        }

        var action = req.IsActive ? AuditEntry.Actions.UserActivated : AuditEntry.Actions.UserDeactivated;
        var auditEntry = AuditEntry.Log(action, user.Id, null, null);
        context.AuditLog.Add(auditEntry);
        await context.SaveChangesAsync(ct);

        await Send.OkAsync(ct);
    }
}
