namespace AQ.Generators.Commenting
{
    internal static class CommentingNames
    {
        public const string CommentableAttribute = "AQ.Utilities.Commenting.CommentableAttribute";
    }

    internal sealed record CommentableModel(
        string FullName,
        string ClassName,
        string Name,
        string Namespace,
        string CommentType,
        EquatableArray<DiagnosticInfo> Diagnostics);
}
