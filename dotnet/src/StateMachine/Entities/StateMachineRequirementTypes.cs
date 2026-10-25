using System.Collections.Concurrent;
using System.Reflection;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Maps a requirement's type name to its type, so stored requirements come back as the type that was saved.
/// An app registers the assemblies that hold its requirements once, before any transition is loaded.
/// </summary>
public static class StateMachineRequirementTypes
{
    private static readonly ConcurrentDictionary<string, Type> _types = new(StringComparer.Ordinal);

    public static void Register(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes().Where(IsRequirementType))
            Register(type);
    }

    public static void Register(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!IsRequirementType(type))
            throw new ArgumentException($"{type.Name} is not a concrete {nameof(IStateMachineTransitionRequirement)}.", nameof(type));

        if (!_types.TryAdd(type.Name, type) && _types[type.Name] != type)
            throw new InvalidOperationException($"A requirement type named '{type.Name}' is already registered.");
    }

    public static bool TryResolve(string typeName, out Type type) => _types.TryGetValue(typeName, out type!);

    public static IReadOnlyCollection<Type> All => _types.Values.ToList();

    private static bool IsRequirementType(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && typeof(IStateMachineTransitionRequirement).IsAssignableFrom(type)
        && type != typeof(UnresolvedStateMachineTransitionRequirement);
}
