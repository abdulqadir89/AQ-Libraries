using System.Collections.Concurrent;
using System.Text.Json;

namespace AQ.Utilities.EntityData;

public static class EntityDataRegistry<TAttribute> where TAttribute : EntityDataMemberAttribute
{
    private static readonly ConcurrentDictionary<Type, Action<object, IDictionary<string, object?>>> Writers = new();
    private static readonly ConcurrentDictionary<Type, Action<object, IDictionary<string, object?>>[]> Chains = new();
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void Register(Type declaringType, Action<object, IDictionary<string, object?>> writer)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(writer);
        Writers[declaringType] = writer;
        Chains.Clear();
    }

    public static bool HasWriters(Type runtimeType) => GetChain(runtimeType).Length > 0;

    public static Dictionary<string, object?> Build(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var data = new Dictionary<string, object?>();
        foreach (var writer in GetChain(entity.GetType()))
        {
            writer(entity, data);
        }

        return data;
    }

    // RULE VSN-1
    public static string Serialize(object entity) => JsonSerializer.Serialize(Build(entity), SerializerOptions);

    private static Action<object, IDictionary<string, object?>>[] GetChain(Type runtimeType) =>
        Chains.GetOrAdd(runtimeType, static type =>
        {
            var chain = new List<Action<object, IDictionary<string, object?>>>();
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (Writers.TryGetValue(current, out var writer))
                {
                    chain.Add(writer);
                }
            }

            chain.Reverse();
            return [.. chain];
        });
}
