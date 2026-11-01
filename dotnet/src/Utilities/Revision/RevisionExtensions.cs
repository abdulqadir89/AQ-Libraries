using AQ.Entities;
using AQ.Utilities.Results;

namespace AQ.Utilities.Revision;

/// <summary>
/// A request that carries the revision of the record the caller was looking at.
/// </summary>
public interface IRevisioned
{
    int Revision { get; }
}

public static class RevisionExtensions
{
    public const string StaleCode = "Revision.Stale";

    /// <summary>
    /// Succeeds when <paramref name="expected"/> is the entity's current revision; otherwise a conflict failure
    /// (HTTP 409) telling the caller the record changed since they loaded it.
    /// </summary>
    public static Result EnsureRevision(this ITimestamped entity, int expected)
    {
        // RULE RVN-1
        return entity.Revision == expected
            ? Result.Success()
            : Result.Failure(Error.Conflict(StaleCode, "This record was changed by someone else since you opened it. Reload it and try again."));
    }
}
