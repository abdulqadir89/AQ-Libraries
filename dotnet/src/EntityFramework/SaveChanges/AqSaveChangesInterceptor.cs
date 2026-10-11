using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AQ.EntityFramework.SaveChanges;

public sealed class AqSaveChangesInterceptor(AqSavePipelineOptions options) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Run(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Run(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Run(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var userId = (context as ICurrentUserProvider)?.CurrentUserId;

        if (options.VerificationReset)
        {
            VerificationTouchStep.Run(context);
        }

        if (options.Lifecycle)
        {
            LifecycleStep.Run(context);
        }

        if (options.GeneratedReferences)
        {
            GeneratedReferencesStep.Run(context);
        }

        if (options.VerificationReset)
        {
            VerificationResetStep.Run(context);
        }

        if (options.Audit)
        {
            AuditStep.Run(context, userId);
        }

        if (options.Snapshots)
        {
            SnapshotStep.Run(context);
        }

        if (options.Outbox)
        {
            OutboxStep.Run(context, userId);
        }

        context.ChangeTracker.DetectChanges();
    }
}
