namespace AQ.Identity.Core.Abstractions;

/// <summary>
/// Extensibility point for consuming apps to react to account deletion (GDPR Art. 17 "right to
/// erasure") and data export (GDPR Art. 20 "right to data portability") requests initiated
/// against this library's generic self-service pages. The library only knows about identity
/// data (claims, sessions, the user record itself); app-specific data (e.g. ELS's course
/// enrollments) is genuinely out of this library's scope and belongs behind this interface,
/// implemented and registered by the consuming app's own DI container.
/// </summary>
public interface IUserDataLifecycleHook
{
    /// <summary>
    /// Called before the user record itself is deleted. Implementations should clean up or
    /// anonymize app-specific data tied to this user. Throwing aborts the deletion.
    /// </summary>
    Task OnBeforeUserDeletedAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Returns app-specific data to merge into the user's data export payload. Return an empty
    /// dictionary if this app has nothing to add.
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>> ExportUserDataAsync(Guid userId, CancellationToken ct);
}
