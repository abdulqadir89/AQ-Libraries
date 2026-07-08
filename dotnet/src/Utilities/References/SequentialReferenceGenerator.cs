namespace AQ.Utilities.References;

/// <summary>
/// Base class for prefix + counter reference generation (e.g. "ACT-0042").
/// Computing the next counter value typically requires database access, which
/// this library does not own — consuming projects must subclass this and
/// implement <see cref="GetNextSequenceValue"/> against their own DbContext.
/// </summary>
public abstract class SequentialReferenceGenerator : IReferenceGenerator
{
    public string Generate(ReferenceGenerationContext context)
    {
        var next = GetNextSequenceValue(context);
        return context.Prefix is { Length: > 0 } prefix ? $"{prefix}-{next:D4}" : next.ToString("D4");
    }

    /// <summary>
    /// Computes the next sequence value for the given context (e.g. via a database query).
    /// </summary>
    /// <param name="context">Contextual information for the generation strategy.</param>
    /// <returns>The next sequence number.</returns>
    protected abstract int GetNextSequenceValue(ReferenceGenerationContext context);
}
