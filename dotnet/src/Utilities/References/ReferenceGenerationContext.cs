namespace AQ.Utilities.References;

/// <summary>
/// Contextual information passed to an <see cref="IReferenceGenerator"/>.
/// </summary>
/// <param name="Prefix">Optional prefix to prepend to the generated reference (e.g. "ACT").</param>
/// <param name="Length">Length of the generated code/slug portion. Required by length-sensitive strategies (e.g. <see cref="ShortCodeReferenceGenerator"/>).</param>
public sealed record ReferenceGenerationContext(string? Prefix = null, int? Length = null);
