using AQ.Identity.Core.Entities;

namespace AQ.Identity.OpenIddict.Management.Dto;

public class AuditEntryDto
{
    public Guid Id { get; set; }
    public AuditActorKind ActorKind { get; set; }
    public Guid? ActorId { get; set; }
    public Guid? SubjectUserId { get; set; }
    public string? ClientId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
