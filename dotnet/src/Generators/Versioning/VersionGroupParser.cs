using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AQ.Generators.Snapshots;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AQ.Generators.Versioning
{
    internal static class VersionGroupParser
    {
        public static VersionGroupModel Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var symbol = (INamedTypeSymbol)context.TargetSymbol;
            var declaration = (ClassDeclarationSyntax)context.TargetNode;
            var attribute = context.Attributes[0];
            var diagnostics = new List<DiagnosticInfo>();
            var reference = attribute.ApplicationSyntaxReference;

            // RULE GEN-2
            if (!declaration.Modifiers.Any(m => m.Text == "partial") || symbol.ContainingType is not null || symbol.IsGenericType)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [VersionGroup] and must be a partial, non-generic, top-level class.",
                    reference));
            }

            if (!symbol.AllInterfaces.Any(i => i.IsInterface("AQ.Entities", "IEntity")))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [VersionGroup] and must implement IEntity.",
                    reference));
            }

            foreach (var reserved in new[] { "VersionGroupId", "Version" })
            {
                if (symbol.DeclaresMember(reserved))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        "AQGEN002",
                        "Class '" + symbol.Name + "' is marked [VersionGroup] and already declares a member named '" + reserved + "', which the generator adds.",
                        reference));
                }
            }

            var snapshotted = false;
            for (var current = symbol; current is not null && !snapshotted; current = current.BaseType)
            {
                snapshotted = current.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == SnapshotNames.SnapshottableAttribute);
            }

            return new VersionGroupModel(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.Name,
                symbol.NamespaceName(),
                symbol.AllInterfaces.Any(i => i.IsInterface("AQ.Utilities.Verification", "IVerifiable")),
                snapshotted,
                diagnostics.ToEquatableArray());
        }
    }
}
