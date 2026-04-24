using System.Text.Json;

namespace AQ.StateMachine.Entities;

/// <summary>
/// Represents a single typed metadata entry stored on a state machine transition.
/// Metadata is stored as an array of entries so multiple process types can contribute
/// data to a single transition.
/// </summary>
public sealed record StateMachineMetadataEntry(string Type, JsonElement Data)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Creates a new metadata entry, serializing <paramref name="data"/> to JSON.
    /// </summary>
    public static StateMachineMetadataEntry Create<T>(string type, T data) where T : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return new StateMachineMetadataEntry(type, JsonSerializer.SerializeToElement(data, SerializerOptions));
    }
}
