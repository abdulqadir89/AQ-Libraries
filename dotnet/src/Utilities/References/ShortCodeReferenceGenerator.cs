using System.Security.Cryptography;

namespace AQ.Utilities.References;

/// <summary>
/// Generates references as an optional prefix followed by a short random
/// Crockford base32 code, e.g. "ACT-7K2QX9". Uses a cryptographically
/// secure RNG so codes are evenly distributed and non-predictable.
/// </summary>
public sealed class ShortCodeReferenceGenerator : IReferenceGenerator
{
    // Crockford base32 alphabet: excludes I, L, O, U to avoid visual ambiguity when typed by hand.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public string Generate(ReferenceGenerationContext context)
    {
        if (context.Length is not > 0)
            throw new ArgumentException("Length must be provided and greater than zero for short code generation.", nameof(context));

        var codeLength = context.Length.Value;
        Span<char> code = stackalloc char[codeLength];
        for (var i = 0; i < codeLength; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        var value = new string(code);
        return context.Prefix is { Length: > 0 } prefix ? $"{prefix}-{value}" : value;
    }
}
