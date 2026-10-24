namespace AQ.Generators.Snapshots
{
    internal static class SnapshotNames
    {
        public const string SnapshotAttribute = "AQ.Utilities.Snapshots.SnapshotAttribute";
        public const string SnapshottableAttribute = "AQ.Utilities.Snapshots.SnapshottableAttribute";
        public const string SnapshotAttributeGlobal = "global::AQ.Utilities.Snapshots.SnapshotAttribute";
    }

    internal sealed record MemberModel(string DeclaringType, string SortKey, EquatableArray<string> Statements);

    internal sealed record MemberResult(MemberModel? Model, EquatableArray<DiagnosticInfo> Diagnostics);

    internal sealed record ClassModel(
        string FullName,
        string Name,
        string Namespace,
        bool IncludeId,
        string? TypeNameOverride,
        int KeepLast,
        int MaxAgeDays,
        EquatableArray<DiagnosticInfo> Diagnostics);
}
