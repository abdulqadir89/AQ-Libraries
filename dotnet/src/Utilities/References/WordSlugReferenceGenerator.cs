using System.Security.Cryptography;

namespace AQ.Utilities.References;

/// <summary>
/// Generates references as an adjective-noun-number slug, e.g. "brave-falcon-42".
/// Intended for cases where a memorable, pronounceable reference is preferred
/// over a compact code.
/// </summary>
public sealed class WordSlugReferenceGenerator : IReferenceGenerator
{
    private static readonly string[] Adjectives =
    [
        "brave", "calm", "eager", "gentle", "happy", "keen", "lively", "mighty",
        "nimble", "proud", "quiet", "rapid", "silent", "swift", "vivid", "witty",
    ];

    private static readonly string[] Nouns =
    [
        "falcon", "harbor", "meadow", "river", "summit", "canyon", "ember", "glacier",
        "lagoon", "orbit", "pebble", "quartz", "ridge", "storm", "thicket", "willow",
    ];

    public string Generate(ReferenceGenerationContext context)
    {
        var adjective = Adjectives[RandomNumberGenerator.GetInt32(Adjectives.Length)];
        var noun = Nouns[RandomNumberGenerator.GetInt32(Nouns.Length)];
        var number = RandomNumberGenerator.GetInt32(100);

        var slug = $"{adjective}-{noun}-{number:D2}";
        return context.Prefix is { Length: > 0 } prefix ? $"{prefix}-{slug}" : slug;
    }
}
