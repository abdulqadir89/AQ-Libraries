using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static AQ.Generators.AttributeReader;

namespace AQ.Generators.Attachments
{
    internal static class AttachableParser
    {
        public static AttachableModel Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var symbol = (INamedTypeSymbol)context.TargetSymbol;
            var attribute = context.Attributes[0];
            var diagnostics = new List<DiagnosticInfo>();
            var reference = attribute.ApplicationSyntaxReference;

            // RULE GEN-12
            if (symbol.ContainingType is not null || symbol.IsGenericType)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] and must be a non-generic, top-level class.",
                    reference));
            }

            if (!symbol.AllInterfaces.Any(i => i.IsInterface("AQ.Entities", "IEntity")))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] and must implement IEntity.",
                    reference));
            }

            var name = GetString(attribute, "Name") ?? symbol.Name;
            var attachmentType = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : null;
            var attachmentTypeName = string.Empty;

            if (attachmentType is null || !DerivesFromAttachmentBase(attachmentType))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] with an attachment type that does not derive from Attachment<TPrincipal>.",
                    reference));
            }
            else
            {
                attachmentTypeName = attachmentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (attachmentType.IsSealed || !HasAccessibleParameterlessConstructor(attachmentType, symbol))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        "AQGEN002",
                        "Class '" + symbol.Name + "' is marked [Attachable] with an attachment type that is sealed or has no accessible parameterless constructor.",
                        reference));
                }

                foreach (var reserved in new[] { name + "Id", name, "Limits" })
                {
                    if (attachmentType.DeclaresMember(reserved))
                    {
                        diagnostics.Add(DiagnosticInfo.Create(
                            "AQGEN002",
                            "Class '" + symbol.Name + "' is marked [Attachable] and its attachment type already declares a member named '" + reserved + "', which the generator adds.",
                            reference));
                    }
                }
            }

            if (!SyntaxFacts.IsValidIdentifier(name) || name == "Entity" || name == "Attachment")
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] with the name '" + name + "', which is not usable as a type and foreign key prefix.",
                    reference));
            }
            else if (symbol.ContainingNamespace.GetTypeMembers(name + "Attachment").Any())
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] and the attachment type '" + name + "Attachment' already exists in its namespace; set a distinct Name.",
                    reference));
            }

            var maxCount = GetInt(attribute, "MaxCount");
            var maxFileSize = GetLong(attribute, "MaxFileSizeBytes");
            if (maxCount < 0 || maxFileSize < 0)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] with a negative MaxCount or MaxFileSizeBytes.",
                    reference));
            }

            var extensions = GetStrings(attribute, "AllowedExtensions");
            if (extensions.Any(e => e.Length < 2 || e[0] != '.' || e != e.ToLowerInvariant()))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN002",
                    "Class '" + symbol.Name + "' is marked [Attachable] with AllowedExtensions that are not lower case and start with a dot.",
                    reference));
            }

            return new AttachableModel(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.Name,
                name,
                symbol.NamespaceName(),
                attachmentTypeName,
                maxCount,
                maxFileSize,
                GetInt(attribute, "ContentKind"),
                GetStrings(attribute, "ExtraContentTypes").ToEquatableArray(),
                extensions.ToEquatableArray(),
                !attribute.NamedArguments.Any(n => n.Key == "CascadeDelete") || GetBool(attribute, "CascadeDelete"),
                diagnostics.ToEquatableArray());
        }

        private static bool DerivesFromAttachmentBase(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                var definition = current.OriginalDefinition;
                if (definition.Name == "Attachment" && definition.TypeArguments.Length == 1 && definition.ContainingNamespace.ToDisplayString() == "AQ.Utilities.Attachments")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol attachmentType, INamedTypeSymbol owner)
        {
            var sameAssembly = SymbolEqualityComparer.Default.Equals(attachmentType.ContainingAssembly, owner.ContainingAssembly);
            return attachmentType.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0
                && (c.DeclaredAccessibility == Accessibility.Public
                    || c.DeclaredAccessibility == Accessibility.Protected
                    || c.DeclaredAccessibility == Accessibility.ProtectedOrInternal
                    || (sameAssembly && (c.DeclaredAccessibility == Accessibility.Internal || c.DeclaredAccessibility == Accessibility.ProtectedAndInternal))));
        }
    }
}
