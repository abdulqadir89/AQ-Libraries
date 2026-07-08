namespace AQ.Utilities.References;

/// <summary>
/// Decorates a property to declare how its default reference value should be
/// generated when left blank. Resolve with <see cref="ReferenceAttributeResolver"/>.
/// </summary>
/// <param name="generatorType">The <see cref="IReferenceGenerator"/> implementation to use. Must have a public parameterless constructor.</param>
/// <param name="length">Length of the generated code/slug portion. Required.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GeneratedReferenceAttribute(Type generatorType, int length) : Attribute
{
    public Type GeneratorType { get; } = generatorType.IsAssignableTo(typeof(IReferenceGenerator))
        ? generatorType
        : throw new ArgumentException($"'{generatorType.Name}' must implement {nameof(IReferenceGenerator)}.", nameof(generatorType));

    public int Length { get; } = length;

    public string? Prefix { get; init; }
}
