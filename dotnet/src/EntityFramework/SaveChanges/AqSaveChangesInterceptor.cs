using AQ.Actors;
using AQ.Entities;
using AQ.Events.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AQ.EntityFramework.SaveChanges;

/// <summary>
/// Runs the save pipeline. When audit or outbox is enabled and the save touches an auditable entity or raises
/// domain events, an actor must be active: a missing actor throws <see cref="MissingActorException"/> instead of
/// storing null attribution.
/// </summary>
public sealed class AqSaveChangesInterceptor(AqSavePipelineOptions options, IActorContext actors) : SaveChangesInterceptor
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

    // RULE EFC-1
    private void Run(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        if (options.Audit || options.Lifecycle || options.Snapshots || options.VerificationReset || options.Review)
        {
            OwnedChangeTouchStep.Run(context);
        }

        // Resolved after the touch step, which turns owned-only changes into owner updates.
        actors.TryGet(out var actor);
        // RULE EFC-2
        if (actor is null && RequiresActor(context))
        {
            throw new MissingActorException();
        }

        if (options.Lifecycle)
        {
            LifecycleStep.Run(context);
        }

        if (options.VersionGroups)
        {
            VersionGroupStep.Run(context);
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
            AuditStep.Run(context, actor);
        }

        if (options.Snapshots)
        {
            SnapshotStep.Run(context);
        }

        if (options.AuditLog)
        {
            AuditLogStep.Run(context, actor);
        }

        if (options.Outbox)
        {
            OutboxStep.Run(context, actor, options.OutboxJson);
        }

        context.ChangeTracker.DetectChanges();
    }

    private bool RequiresActor(DbContext context)
    {
        if (!options.Audit && !options.Outbox && !options.AuditLog)
        {
            return false;
        }

        if (options.AuditLog && AuditLogStep.HasAuditedChange(context))
        {
            return true;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var changed = entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

            if (options.Audit && changed && entry.Entity is IAuditable)
            {
                return true;
            }

            if (options.Outbox && entry.Entity is IHasDomainEvents events && events.DomainEvents.Count > 0)
            {
                return true;
            }
        }

        return false;
    }
}
