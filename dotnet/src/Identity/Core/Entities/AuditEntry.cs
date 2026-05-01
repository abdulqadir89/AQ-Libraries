namespace AQ.Identity.Core.Entities;

public class AuditEntry
{
    public Guid Id { get; private set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

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
    }

    public AuditEntry() { }

    public AuditEntry(string action, Guid? userId, string? ip, string? ua)
    {
        Id = Guid.NewGuid();
        Action = action;
        UserId = userId;
        IpAddress = ip;
        UserAgent = ua;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public static AuditEntry Log(string action, Guid? userId, string? ip, string? ua)
    {
        return new AuditEntry(action, userId, ip, ua);
    }
}
