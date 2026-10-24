using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static AQ.Generators.AttributeReader;

namespace AQ.Generators.Tagging
{
    internal static class TaggableParser
    {
        public static TaggableModel Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var symbol = (INamedTypeSymbol)context.TargetSymbol;
            var declaration = (ClassDeclarationSyntax)context.TargetNode;
            var attribute = context.Attributes[0];
            var diagnostics = new List<DiagnosticInfo>();
            var reference = attribute.ApplicationSyntaxReference;

            // RULE GEN-8
            if (!declaration.Modifiers.Any(m => m.Text == "partial") || symbol.ContainingType is not null || symbol.IsGenericType)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Taggable] and must be a partial, non-generic, top-level class.",
                    reference));
            }

            if (!symbol.AllInterfaces.Any(i => i.IsInterface("AQ.Entities", "IEntity")))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Taggable] and must implement IEntity.",
                    reference));
            }

            foreach (var reserved in new[] { "Tags", "TagIds" })
            {
                if (symbol.DeclaresMember(reserved))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        "AQGEN002",
                        "Class '" + symbol.Name + "' is marked [Taggable] and already declares a member named '" + reserved + "', which the generator adds.",
                        reference));
                }
            }

            var tagType = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : null;
            var tagTypeName = string.Empty;
            if (tagType is null || !tagType.AllInterfaces.Any(i => i.IsInterface("AQ.Utilities.Tagging", "ITag")))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Taggable] with a tag type that does not implement ITag.",
                    reference));
            }
            else
            {
                tagTypeName = tagType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            var name = GetString(attribute, "Name") ?? symbol.Name;
            if (!SyntaxFacts.IsValidIdentifier(name) || name == "Entity" || name == "Tag")
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Taggable] with the name '" + name + "', which is not usable as a type and foreign key prefix.",
                    reference));
            }
            else if (symbol.ContainingNamespace.GetTypeMembers(name + "Tag").Any())
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Taggable] and the link type '" + name + "Tag' already exists in its namespace; set a distinct Name.",
                    reference));
            }

            return new TaggableModel(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.Name,
                name,
                symbol.NamespaceName(),
                tagTypeName,
                diagnostics.ToEquatableArray());
        }
    }
}
