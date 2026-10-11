using AQ.Entities;

namespace AQ.Utilities.Versioning;

public abstract class EntityVersion<TUser> : Entity<TUser> where TUser : class
{
    public int VersionNumber { get; private set; }
    public string? Label { get; private set; }
    public string Data { get; private set; } = string.Empty;
    public int? SourceVersionNumber { get; private set; }

    public abstract Guid GetEntityId();

    protected void Initialize(int versionNumber, string? label, string data, int? sourceVersionNumber)
    {
        ArgumentNullException.ThrowIfNull(data);
        VersionNumber = versionNumber;
        Label = label;
        Data = data;
        SourceVersionNumber = sourceVersionNumber;
    }
}
