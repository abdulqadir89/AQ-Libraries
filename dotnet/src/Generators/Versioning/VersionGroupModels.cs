namespace AQ.Generators.Versioning
{
    internal static class VersioningNames
    {
        public const string VersionGroupAttribute = "AQ.Utilities.Versioning.VersionGroupAttribute";
    }

    internal sealed record VersionGroupModel(
        string FullName,
        string Name,
        string Namespace,
        bool Verifiable,
        bool Snapshotted,
        EquatableArray<DiagnosticInfo> Diagnostics);
}
