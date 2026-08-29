using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Entities;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AQ.Identity.OpenIddict.Management.Endpoints.Users;

public class DeleteUserRequest
{
    public Guid Id { get; set; }
}

public class DeleteUserEndpoint(
    UserManager<ApplicationUser> userManager,
    IIdentityDbContext context,
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IEnumerable<IUserDataLifecycleHook> lifecycleHooks)
    : Endpoint<DeleteUserRequest>
{
    public override void Configure()
    {
        Delete("/manage/users/{Id}");
        Policies("ManageApi");
    }

    public override async Task HandleAsync(DeleteUserRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id.ToString());
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var holdsAdminClaim = await context.StoredClaims
            .AnyAsync(c => c.UserId == req.Id && c.Type == AdminClaimGuard.ManageApiClaimType, ct);

        if (holdsAdminClaim && await AdminClaimGuard.WouldRemoveLastAdminAsync(context, req.Id, ct))
        {
            AddError("Cannot delete the last administrator.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        foreach (var hook in lifecycleHooks)
        {
            await hook.OnBeforeUserDeletedAsync(req.Id, ct);
        }

        await authorizationManager.RevokeBySubjectAsync(req.Id.ToString(), ct);
        var tokens = tokenManager.FindBySubjectAsync(req.Id.ToString(), ct);
        await foreach (var token in tokens)
        {
            await tokenManager.TryRevokeAsync(token, ct);
        }

        var claims = context.StoredClaims.Where(c => c.UserId == req.Id);
        context.StoredClaims.RemoveRange(claims);

        context.AuditLog.Add(AuditEntry.Log(AuditEntry.Actions.AccountDeleted, req.Id, null, null));
        await context.SaveChangesAsync(ct);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            ThrowError("Failed to delete user");
        }

        await Send.NoContentAsync(ct);
    }
}
