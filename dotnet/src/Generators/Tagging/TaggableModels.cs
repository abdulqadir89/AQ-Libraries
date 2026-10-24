namespace AQ.Generators.Tagging
{
    internal static class TaggingNames
    {
        public const string TaggableAttribute = "AQ.Utilities.Tagging.TaggableAttribute";
    }

    internal sealed record TaggableModel(
        string FullName,
        string ClassName,
        string Name,
        string Namespace,
        string TagType,
        EquatableArray<DiagnosticInfo> Diagnostics);
}
