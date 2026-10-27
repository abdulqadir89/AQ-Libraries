using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static AQ.Generators.AttributeReader;

namespace AQ.Generators.Commenting
{
    internal static class CommentableParser
    {
        public static CommentableModel Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var symbol = (INamedTypeSymbol)context.TargetSymbol;
            var attribute = context.Attributes[0];
            var diagnostics = new List<DiagnosticInfo>();
            var reference = attribute.ApplicationSyntaxReference;

            // RULE GEN-10
            if (symbol.ContainingType is not null || symbol.IsGenericType)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Commentable] and must be a non-generic, top-level class.",
                    reference));
            }

            if (!symbol.AllInterfaces.Any(i => i.IsInterface("AQ.Entities", "IEntity")))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Commentable] and must implement IEntity.",
                    reference));
            }

            var name = GetString(attribute, "Name") ?? symbol.Name;
            var commentType = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : null;
            var commentTypeName = string.Empty;

            if (commentType is null || !DerivesFromCommentBase(commentType))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Commentable] with a comment type that does not derive from Comment<TPrincipal>.",
                    reference));
            }
            else
            {
                commentTypeName = commentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (commentType.IsSealed || !HasAccessibleParameterlessConstructor(commentType, symbol))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        "AQGEN002",
                        "Class '" + symbol.Name + "' is marked [Commentable] with a comment type that is sealed or has no accessible parameterless constructor.",
                        reference));
                }

                foreach (var reserved in new[] { name + "Id", name })
                {
                    if (commentType.DeclaresMember(reserved))
                    {
                        diagnostics.Add(DiagnosticInfo.Create(
                            "AQGEN002",
                            "Class '" + symbol.Name + "' is marked [Commentable] and its comment type already declares a member named '" + reserved + "', which the generator adds.",
                            reference));
                    }
                }
            }

            if (!SyntaxFacts.IsValidIdentifier(name) || name == "Entity" || name == "Comment")
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Commentable] with the name '" + name + "', which is not usable as a type and foreign key prefix.",
                    reference));
            }
            else if (symbol.ContainingNamespace.GetTypeMembers(name + "Comment").Any())
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Commentable] and the comment type '" + name + "Comment' already exists in its namespace; set a distinct Name.",
                    reference));
            }

            return new CommentableModel(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.Name,
                name,
                symbol.NamespaceName(),
                commentTypeName,
                diagnostics.ToEquatableArray());
        }

        private static bool DerivesFromCommentBase(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                var definition = current.OriginalDefinition;
                if (definition.Name == "Comment" && definition.TypeArguments.Length == 1 && definition.ContainingNamespace.ToDisplayString() == "AQ.Utilities.Commenting")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol commentType, INamedTypeSymbol owner)
        {
            var sameAssembly = SymbolEqualityComparer.Default.Equals(commentType.ContainingAssembly, owner.ContainingAssembly);
            return commentType.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0
                && (c.DeclaredAccessibility == Accessibility.Public
                    || c.DeclaredAccessibility == Accessibility.Protected
                    || c.DeclaredAccessibility == Accessibility.ProtectedOrInternal
                    || (sameAssembly && (c.DeclaredAccessibility == Accessibility.Internal || c.DeclaredAccessibility == Accessibility.ProtectedAndInternal))));
        }
    }
}
