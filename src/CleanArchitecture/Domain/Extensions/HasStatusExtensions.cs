using AQ.Domain.Entities;

namespace AQ.Domain.Extensions;

public static class HasStatusExtensions
{
    public static bool IsStatus<TStatus>(
        this IHasStatus<TStatus> entity,
        TStatus status
    )
    {
        ArgumentNullException.ThrowIfNull(entity);
        return EqualityComparer<TStatus>.Default.Equals(entity.Status, status);
    }

    public static bool IsInStatus<TStatus>(
        this IHasStatus<TStatus> entity,
        params TStatus[] statuses
    )
    {
        ArgumentNullException.ThrowIfNull(entity);
        return statuses.Contains(entity.Status);
    }

    public static void ChangeStatus<TStatus>(
        this IHasStatus<TStatus> entity,
        TStatus newStatus
    )
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!EqualityComparer<TStatus>.Default.Equals(entity.Status, newStatus))
        {
            entity.SetStatus(newStatus);
        }
    }

    // Special helpers for boolean status
    public static bool IsActive(this IHasStatus<bool> entity) => entity.Status;
    public static bool IsInactive(this IHasStatus<bool> entity) => !entity.Status;

    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query)
    where T : class, IHasStatus<bool>
    => query.Where(e => e.Status);

    public static IQueryable<T> WhereInactive<T>(this IQueryable<T> query)
        where T : class, IHasStatus<bool>
        => query.Where(e => !e.Status);
}
