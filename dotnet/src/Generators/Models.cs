using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AQ.Generators
{
    internal static class Names
    {
        public const string SnapshotAttribute = "AQ.Utilities.Snapshots.SnapshotAttribute";
        public const string SnapshottableAttribute = "AQ.Utilities.Snapshots.SnapshottableAttribute";
        public const string VersionedAttribute = "AQ.Utilities.Versioning.VersionedAttribute";
        public const string VersionableAttribute = "AQ.Utilities.Versioning.VersionableAttribute";

        public const string EntityDataRegistry = "global::AQ.Utilities.EntityData.EntityDataRegistry";
        public const string SnapshotAttributeGlobal = "global::AQ.Utilities.Snapshots.SnapshotAttribute";
        public const string VersionedAttributeGlobal = "global::AQ.Utilities.Versioning.VersionedAttribute";

        public static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                var hasNext = i + 1 < chars.Length;
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }

            return new string(chars);
        }

        public static string SanitizeNamespace(string assemblyName)
        {
            var builder = new StringBuilder(assemblyName.Length + 1);
            foreach (var c in assemblyName)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '.' ? c : '_');
            }

            var result = builder.ToString().Trim('.');
            if (result.Length == 0)
            {
                return "_";
            }

            var segments = result.Split('.');
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0 || char.IsDigit(segments[i][0]))
                {
                    segments[i] = "_" + segments[i];
                }
            }

            return string.Join(".", segments);
        }
    }

    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor NoDecoratedMembers = new DiagnosticDescriptor(
            "AQGEN001",
            "Class has no decorated members",
            "{0}",
            "AQ.Generators",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingAuditable = new DiagnosticDescriptor(
            "AQGEN002",
            "Versionable class is not auditable",
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

        public static DiagnosticDescriptor ById(string id)
        {
            switch (id)
            {
                case "AQGEN001":
                    return NoDecoratedMembers;
                case "AQGEN002":
                    return MissingAuditable;
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

    internal sealed record MemberModel(string DeclaringType, string SortKey, EquatableArray<string> Statements);

    internal sealed record MemberResult(MemberModel? Model, EquatableArray<DiagnosticInfo> Diagnostics);

    internal sealed record ClassModel(
        string FullName,
        string Name,
        string Namespace,
        bool IncludeId,
        string? TypeNameOverride,
        string? UserType,
        EquatableArray<DiagnosticInfo> Diagnostics);

    internal sealed record GenerationInput(
        string AssemblyName,
        EquatableArray<MemberResult> SnapshotMembers,
        EquatableArray<MemberResult> VersionedMembers,
        EquatableArray<ClassModel> Snapshottable,
        EquatableArray<ClassModel> Versionable);
}
