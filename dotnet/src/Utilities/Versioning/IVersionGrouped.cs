using AQ.Entities;

namespace AQ.Utilities.Versioning;

public interface IVersionGrouped : IEntity
{
    Guid VersionGroupId { get; }
    string? Version { get; }
    void SetVersionGroup(Guid versionGroupId, string? version);
}
