namespace AQ.Utilities.Versioning;

public static class VersionGroupOperations
{
    public const int MaxVersionLength = 100;

    /// <summary>Puts <paramref name="entity"/> into the group of <paramref name="target"/> under <paramref name="version"/>. The caller checks that the version is free in that group.</summary>
    // RULE VSN-4
    public static void JoinVersionGroup<T>(this T entity, T target, string version)
        where T : IVersionGrouped
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(target);
        var label = NormalizeVersion(version);

        if (entity.Id == target.Id)
        {
            throw new InvalidOperationException("An entity cannot join its own version group through itself.");
        }

        if (entity.VersionGroupId == target.VersionGroupId)
        {
            throw new InvalidOperationException($"Entity {entity.Id} is already in version group {target.VersionGroupId}.");
        }

        entity.SetVersionGroup(target.VersionGroupId, label);
    }

    /// <summary>Moves <paramref name="entity"/> into a new group of its own, keeping its version label.</summary>
    // RULE VSN-5
    public static void LeaveVersionGroup<T>(this T entity)
        where T : IVersionGrouped
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.SetVersionGroup(Guid.CreateVersion7(), entity.Version);
    }

    /// <summary>Changes the version label inside the entity's current group. The caller checks that the label is free in that group.</summary>
    // RULE VSN-6
    public static void SetVersionLabel<T>(this T entity, string version)
        where T : IVersionGrouped
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.SetVersionGroup(entity.VersionGroupId, NormalizeVersion(version));
    }

    // RULE VSN-7
    public static string NormalizeVersion(string version)
    {
        var trimmed = version?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("A version label is required.", nameof(version));
        }

        if (trimmed.Length > MaxVersionLength)
        {
            throw new ArgumentException($"A version label is at most {MaxVersionLength} characters.", nameof(version));
        }

        return trimmed;
    }
}
