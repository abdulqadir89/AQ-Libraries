using System.Text.Json;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Stores requirements as one JSON object keyed by type name and reads them back as the registered type.
/// ADR-003: registered types, not an internal wrapper, so handlers match what was saved.
/// </summary>
public static class StateMachineRequirementSerializer
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    // RULE SMC-11
    public static string? Serialize(IEnumerable<IStateMachineTransitionRequirement>? requirements)
    {
        var items = requirements?.ToList();
        if (items is null || items.Count == 0) return null;

        var stored = new Dictionary<string, JsonElement>();
        foreach (var requirement in items)
        {
            var (name, data) = requirement switch
            {
                UnresolvedStateMachineTransitionRequirement unresolved => (unresolved.RequirementTypeName, JsonSerializer.Deserialize<JsonElement>(unresolved.DataJson)),
                _ => (requirement.GetType().Name, JsonSerializer.SerializeToElement(requirement, requirement.GetType(), _options))
            };

            // RULE SMC-13
            if (!stored.TryAdd(name, data))
                throw new InvalidOperationException($"A transition can hold only one '{name}' requirement.");
        }

        return JsonSerializer.Serialize(stored);
    }

    // RULE SMC-12
    public static IEnumerable<IStateMachineTransitionRequirement>? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        var stored = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? throw new JsonException("Stored requirements are not a JSON object.");

        return stored
            .Select(item => StateMachineRequirementTypes.TryResolve(item.Key, out var type)
                ? (IStateMachineTransitionRequirement)(item.Value.Deserialize(type, _options)
                    ?? throw new JsonException($"Stored requirement '{item.Key}' is empty."))
                : new UnresolvedStateMachineTransitionRequirement(item.Key, item.Value.GetRawText()))
            .ToList();
    }
}
