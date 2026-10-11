using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace AQ.Generators
{
    internal static class Parser
    {
        public static MemberResult ParseMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var property = (IPropertySymbol)context.TargetSymbol;
            var attribute = context.Attributes[0];
            var containing = property.ContainingType;
            if (property.IsStatic || property.IsIndexer || containing.IsGenericType)
            {
                return new MemberResult(null, default);
            }

            var diagnostics = new List<DiagnosticInfo>();
            var reference = attribute.ApplicationSyntaxReference;
            var attributeName = attribute.AttributeClass?.Name ?? "Attribute";

            var name = GetString(attribute, "Name");
            var member = GetString(attribute, "Member");
            var include = GetStrings(attribute, "Include");
            var nested = GetStrings(attribute, "Nested");
            var asString = GetBool(attribute, "AsString");
            var enumAsNumber = GetBool(attribute, "EnumAsNumber");

            var type = property.Type;
            var isNullableValue = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
            var underlying = isNullableValue ? ((INamedTypeSymbol)type).TypeArguments[0] : type;
            var canBeNull = isNullableValue || !type.IsValueType;
            var op = canBeNull ? "?." : ".";
            var key = name ?? Names.ToCamelCase(property.Name);
            var access = "e." + property.Name;
            var statements = new List<string>();

            if (include.Length > 0)
            {
                foreach (var included in include)
                {
                    if (!HasMember(underlying, included))
                    {
                        diagnostics.Add(UnknownMember(reference, attributeName, "Include", included, property, underlying));
                        continue;
                    }

                    statements.Add("d[\"" + key + included + "\"] = " + access + op + included + ";");
                }
            }
            else if (nested.Length > 0)
            {
                var entries = new List<string>();
                foreach (var nestedName in nested)
                {
                    if (!HasMember(underlying, nestedName))
                    {
                        diagnostics.Add(UnknownMember(reference, attributeName, "Nested", nestedName, property, underlying));
                        continue;
                    }

                    var inner = isNullableValue ? access + ".Value." + nestedName : access + "." + nestedName;
                    entries.Add("[\"" + Names.ToCamelCase(nestedName) + "\"] = " + inner);
                }

                var dictionary = "new global::System.Collections.Generic.Dictionary<string, object?> { " + string.Join(", ", entries) + " }";
                statements.Add(canBeNull
                    ? "d[\"" + key + "\"] = " + access + " != null ? " + dictionary + " : null;"
                    : "d[\"" + key + "\"] = " + dictionary + ";");
            }
            else if (member is not null)
            {
                if (!HasMember(underlying, member))
                {
                    diagnostics.Add(UnknownMember(reference, attributeName, "Member", member, property, underlying));
                }
                else
                {
                    statements.Add("d[\"" + key + "\"] = " + access + op + member + ";");
                }
            }
            else if (asString)
            {
                statements.Add("d[\"" + key + "\"] = " + access + op + "ToString();");
            }
            else if (underlying.TypeKind == TypeKind.Enum && !enumAsNumber)
            {
                statements.Add("d[\"" + key + "\"] = " + access + op + "ToString();");
            }
            else
            {
                statements.Add("d[\"" + key + "\"] = " + access + ";");
            }

            var declaringType = containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var location = property.Locations.FirstOrDefault();
            var sortKey = (location?.SourceTree?.FilePath ?? string.Empty) + ":" + (location?.SourceSpan.Start ?? 0).ToString("D8");
            var model = statements.Count == 0 ? null : new MemberModel(declaringType, sortKey, statements.ToEquatableArray());
            return new MemberResult(model, diagnostics.ToEquatableArray());
        }

        public static ClassModel ParseClass(GeneratorAttributeSyntaxContext context, string memberAttributeName, bool requiresAuditable, CancellationToken cancellationToken)
        {
            var symbol = (INamedTypeSymbol)context.TargetSymbol;
            var attribute = context.Attributes[0];
            var diagnostics = new List<DiagnosticInfo>();
            var reference = attribute.ApplicationSyntaxReference;

            var hasMembers = false;
            for (var current = symbol; current is not null && !hasMembers; current = current.BaseType)
            {
                hasMembers = current.GetMembers().OfType<IPropertySymbol>().Any(p => p.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == memberAttributeName));
            }

            if (!hasMembers)
            {
                var attributeName = attribute.AttributeClass?.Name ?? "Attribute";
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN001",
                    "Class '" + symbol.Name + "' is marked [" + attributeName.Replace("Attribute", string.Empty) + "] but has no members decorated with [" + memberAttributeName.Substring(memberAttributeName.LastIndexOf('.') + 1).Replace("Attribute", string.Empty) + "] in its hierarchy.",
                    reference));
            }

            string? userType = null;
            if (requiresAuditable)
            {
                var auditable = symbol.AllInterfaces.FirstOrDefault(i => i.Name == "IAuditable" && i.TypeArguments.Length == 1);
                if (auditable is null)
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        "AQGEN002",
                        "Class '" + symbol.Name + "' is marked [Versionable] but does not implement IAuditable<TUser>.",
                        reference));
                }
                else
                {
                    userType = auditable.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }

            var ns = symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToDisplayString();
            return new ClassModel(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.Name,
                ns,
                GetBool(attribute, "IncludeId"),
                GetString(attribute, "SnapshotTypeName"),
                userType,
                diagnostics.ToEquatableArray());
        }

        private static DiagnosticInfo UnknownMember(SyntaxReference? reference, string attributeName, string option, string memberName, IPropertySymbol property, ITypeSymbol type)
        {
            return DiagnosticInfo.Create(
                "AQGEN003",
                "[" + attributeName.Replace("Attribute", string.Empty) + "] " + option + " member '" + memberName + "' does not exist on type '" + type.ToDisplayString() + "' (property '" + property.Name + "').",
                reference);
        }

        private static bool HasMember(ITypeSymbol type, string name)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.GetMembers(name).Any(m => m is IPropertySymbol or IFieldSymbol))
                {
                    return true;
                }
            }

            return type.AllInterfaces.Any(i => i.GetMembers(name).Any(m => m is IPropertySymbol));
        }

        private static string? GetString(AttributeData attribute, string name)
        {
            foreach (var pair in attribute.NamedArguments)
            {
                if (pair.Key == name)
                {
                    return pair.Value.Value as string;
                }
            }

            return null;
        }

        private static bool GetBool(AttributeData attribute, string name)
        {
            foreach (var pair in attribute.NamedArguments)
            {
                if (pair.Key == name)
                {
                    return pair.Value.Value is true;
                }
            }

            return false;
        }

        private static string[] GetStrings(AttributeData attribute, string name)
        {
            foreach (var pair in attribute.NamedArguments)
            {
                if (pair.Key == name && pair.Value.Kind == TypedConstantKind.Array)
                {
                    return pair.Value.Values.Select(v => v.Value as string).Where(v => v is not null).Select(v => v!).ToArray();
                }
            }

            return new string[0];
        }
    }
}
