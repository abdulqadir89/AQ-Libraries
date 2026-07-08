using System.Linq.Expressions;
using System.Reflection;

namespace AQ.Utilities.References;

/// <summary>
/// Resolves a property's <see cref="GeneratedReferenceAttribute"/> into a generated reference value.
/// </summary>
public static class ReferenceAttributeResolver
{
    /// <summary>
    /// Reads the <see cref="GeneratedReferenceAttribute"/> off the property referenced by <paramref name="propertyExpression"/>,
    /// instantiates its declared generator, and generates a reference value.
    /// </summary>
    /// <param name="propertyExpression">An expression selecting the decorated property, e.g. <c>a =&gt; a.Reference</c>.</param>
    /// <exception cref="InvalidOperationException">The property is not decorated with <see cref="GeneratedReferenceAttribute"/>.</exception>
    public static string Generate<T>(Expression<Func<T, object?>> propertyExpression)
    {
        var attribute = GetAttribute(propertyExpression);
        var generator = (IReferenceGenerator)Activator.CreateInstance(attribute.GeneratorType)!;
        return generator.Generate(new ReferenceGenerationContext(Prefix: attribute.Prefix, Length: attribute.Length));
    }

    /// <summary>
    /// Reads the <see cref="GeneratedReferenceAttribute"/> off the property referenced by <paramref name="propertyExpression"/>
    /// and builds the corresponding <see cref="ReferenceGenerationContext"/>, without generating a value.
    /// </summary>
    /// <param name="propertyExpression">An expression selecting the decorated property, e.g. <c>a =&gt; a.Reference</c>.</param>
    /// <exception cref="InvalidOperationException">The property is not decorated with <see cref="GeneratedReferenceAttribute"/>.</exception>
    public static ReferenceGenerationContext ResolveContext<T>(Expression<Func<T, object?>> propertyExpression)
    {
        var attribute = GetAttribute(propertyExpression);
        return new ReferenceGenerationContext(Prefix: attribute.Prefix, Length: attribute.Length);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, (PropertyInfo Property, GeneratedReferenceAttribute Attribute)[]> DecoratedPropertiesByType = new();

    /// <summary>
    /// Fills in every string property on <paramref name="entity"/> decorated with <see cref="GeneratedReferenceAttribute"/>
    /// that is currently null or blank. Intended for a single generic hook (e.g. DbContext.SaveChanges) so individual
    /// entities/services never need to call the generator themselves.
    /// </summary>
    /// <param name="entity">The entity instance to fill in.</param>
    public static void ApplyGeneratedReferences(object entity)
    {
        var decoratedProperties = DecoratedPropertiesByType.GetOrAdd(entity.GetType(), static type =>
            [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite)
                .Select(p => (Property: p, Attribute: p.GetCustomAttribute<GeneratedReferenceAttribute>()))
                .Where(x => x.Attribute is not null)
                .Select(x => (x.Property, Attribute: x.Attribute!))]);

        foreach (var (property, attribute) in decoratedProperties)
        {
            if (property.GetValue(entity) is string existing && !string.IsNullOrWhiteSpace(existing))
                continue;

            var generator = (IReferenceGenerator)Activator.CreateInstance(attribute.GeneratorType)!;
            var value = generator.Generate(new ReferenceGenerationContext(Prefix: attribute.Prefix, Length: attribute.Length));
            property.SetValue(entity, value);
        }
    }

    private static GeneratedReferenceAttribute GetAttribute<T>(Expression<Func<T, object?>> propertyExpression)
    {
        var property = GetPropertyInfo(propertyExpression);
        return property.GetCustomAttribute<GeneratedReferenceAttribute>()
            ?? throw new InvalidOperationException($"Property '{property.Name}' on '{property.DeclaringType?.Name}' is not decorated with [{nameof(GeneratedReferenceAttribute)}].");
    }

    private static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object?>> propertyExpression)
    {
        var body = propertyExpression.Body is UnaryExpression unary ? unary.Operand : propertyExpression.Body;
        if (body is MemberExpression { Member: PropertyInfo property })
            return property;

        throw new ArgumentException("Expression must select a property.", nameof(propertyExpression));
    }
}
