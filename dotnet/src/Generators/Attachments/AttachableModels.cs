namespace AQ.Generators.Attachments
{
    internal static class AttachingNames
    {
        public const string AttachableAttribute = "AQ.Utilities.Attachments.AttachableAttribute";
    }

    internal sealed record AttachableModel(
        string FullName,
        string ClassName,
        string Name,
        string Namespace,
        string AttachmentType,
        int MaxCount,
        long MaxFileSizeBytes,
        int ContentKind,
        EquatableArray<string> ExtraContentTypes,
        EquatableArray<string> AllowedExtensions,
        bool CascadeDelete,
        EquatableArray<DiagnosticInfo> Diagnostics);
}
