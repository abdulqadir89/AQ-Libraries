using System.Collections.Concurrent;
using System.Reflection;

namespace AQ.Utilities.Verification;

public static class VerifiableAttributeResolver
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> DecoratedPropertiesByType = new();

    private static PropertyInfo[] GetDecoratedProperties(Type type) =>
        DecoratedPropertiesByType.GetOrAdd(type, static t =>
            [.. t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<VerifiableAttribute>() is not null)]);

    public static bool HasVerifiableProperties(Type entityType) =>
        GetDecoratedProperties(entityType).Length > 0;

    public static bool AnyVerifiablePropertyModified(Type entityType, Func<string, bool> isPropertyModified) =>
        GetDecoratedProperties(entityType).Any(p => isPropertyModified(p.Name));
}
