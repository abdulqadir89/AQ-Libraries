using AQ.Actors;

namespace AQ.Identity.Core.Entities;

/// <summary>Who performed an audited action. <see cref="Anonymous"/> is the only kind without an <see cref="AuditEntry.ActorId"/>.</summary>
public enum AuditActorKind
{
    Anonymous = 0,
    Human = 1,
    Service = 2,
    System = 3,
}

/// <summary>
/// One audited identity event. The actor (who did it) is recorded separately from the subject user (who it was done to).
/// Create entries only through the factory methods, which enforce that every non-anonymous entry names its actor.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; private set; }

    public AuditActorKind ActorKind { get; private set; }

    /// <summary>Principal that performed the action. Null only when <see cref="ActorKind"/> is <see cref="AuditActorKind.Anonymous"/>.</summary>
    public Guid? ActorId { get; private set; }

    /// <summary>User the action was applied to, when there is one (e.g. the account whose claims changed).</summary>
    public Guid? SubjectUserId { get; private set; }

    /// <summary>OAuth client id of the actor, when it came from a token.</summary>
    public string? ClientId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static class Actions
    {
        public const string LoginSuccess = "LoginSuccess";
        public const string LoginFailed = "LoginFailed";
        public const string LoginLocked = "LoginLocked";
        public const string PasswordChanged = "PasswordChanged";
        public const string PasswordReset = "PasswordReset";
        public const string EmailVerified = "EmailVerified";
        public const string MfaEnabled = "MfaEnabled";
        public const string MfaDisabled = "MfaDisabled";
        public const string KeyRotated = "KeyRotated";
        public const string UserActivated = "UserActivated";
        public const string UserDeactivated = "UserDeactivated";
        public const string UserSessionsRevoked = "UserSessionsRevoked";
        public const string ClientCreated = "ClientCreated";
        public const string ClientUpdated = "ClientUpdated";
        public const string ClientDeleted = "ClientDeleted";
        public const string ClientSecretReset = "ClientSecretReset";
        public const string AdminUserCreated = "AdminUserCreated";
        public const string UserTokensInvalidated = "UserTokensInvalidated";
        public const string UserClaimAdded = "UserClaimAdded";
        public const string UserClaimRemoved = "UserClaimRemoved";
        public const string RefreshTokenReuseDetected = "RefreshTokenReuseDetected";
        public const string ScopeCreated = "ScopeCreated";
        public const string ScopeUpdated = "ScopeUpdated";
        public const string ScopeDeleted = "ScopeDeleted";
        public const string EmailChanged = "EmailChanged";
        public const string BackupCodesRegenerated = "BackupCodesRegenerated";
        public const string AccountDeleted = "AccountDeleted";
    }

    private AuditEntry() { }

    /// <summary>Records an action performed by an authenticated or platform actor.</summary>
    // RULE IDC-2
    public static AuditEntry Log(string action, Actor actor, Guid? subjectUserId = null, string? ip = null, string? ua = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(actor);

        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            ActorKind = actor.Kind switch
            {
                PrincipalKind.Human => AuditActorKind.Human,
                PrincipalKind.Service => AuditActorKind.Service,
                PrincipalKind.System => AuditActorKind.System,
                _ => throw new ArgumentOutOfRangeException(nameof(actor), actor.Kind, "Unknown principal kind."),
            },
            ActorId = actor.PrincipalId,
            ClientId = actor.ClientId,
            SubjectUserId = subjectUserId,
            Action = action,
            IpAddress = ip,
            UserAgent = ua,
            OccurredAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Records an action by someone who is not signed in (failed login, first-run setup, link click).</summary>
    // RULE IDC-2
    public static AuditEntry LogAnonymous(string action, Guid? subjectUserId, string? ip = null, string? ua = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            ActorKind = AuditActorKind.Anonymous,
            SubjectUserId = subjectUserId,
            Action = action,
            IpAddress = ip,
            UserAgent = ua,
            OccurredAt = DateTimeOffset.UtcNow,
        };
    }
}