using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators
{
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor NoDecoratedMembers = new DiagnosticDescriptor(
            "AQGEN001",
            "Class has no decorated members",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidVersionGroupClass = new DiagnosticDescriptor(
            "AQGEN002",
            "Version group class cannot be generated",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnknownMember = new DiagnosticDescriptor(
            "AQGEN003",
            "Referenced member does not exist",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NegativeRetention = new DiagnosticDescriptor(
            "AQGEN004",
            "Snapshot retention is negative",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateSnapshotName = new DiagnosticDescriptor(
            "AQGEN005",
            "Snapshottable classes share a name",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateTaggableName = new DiagnosticDescriptor(
            "AQGEN006",
            "Taggable classes share a name",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ById(string id)
        {
            switch (id)
            {
                case "AQGEN006":
                    return DuplicateTaggableName;
                case "AQGEN005":
                    return DuplicateSnapshotName;
                case "AQGEN004":
                    return NegativeRetention;
                case "AQGEN001":
                    return NoDecoratedMembers;
                case "AQGEN002":
                    return InvalidVersionGroupClass;
                default:
                    return UnknownMember;
            }
        }
    }

    internal sealed record DiagnosticInfo(string Id, string Message, string FilePath, TextSpan Span, LinePositionSpan LineSpan)
    {
        public static DiagnosticInfo Create(string id, string message, SyntaxReference? reference)
        {
            if (reference is null)
            {
                return new DiagnosticInfo(id, message, string.Empty, default, default);
            }

            var tree = reference.SyntaxTree;
            return new DiagnosticInfo(id, message, tree.FilePath, reference.Span, tree.GetLineSpan(reference.Span).Span);
        }

        public Diagnostic ToDiagnostic()
        {
            var location = FilePath.Length == 0 ? Location.None : Location.Create(FilePath, Span, LineSpan);
            return Diagnostic.Create(DiagnosticDescriptors.ById(Id), location, Message);
        }
    }
}
