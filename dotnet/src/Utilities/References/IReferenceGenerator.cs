namespace AQ.Utilities.References;

public interface IReferenceGenerator
{
    /// <summary>
    /// Generates a human-readable, typeable reference string.
    /// </summary>
    /// <param name="context">Contextual information for the generation strategy.</param>
    /// <returns>The generated reference.</returns>
    string Generate(ReferenceGenerationContext context);
}
