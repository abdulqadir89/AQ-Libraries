using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using static AQ.Generators.AttributeReader;

namespace AQ.Generators.Snapshots
{
    internal static class SnapshotParser
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
            else if (!enumAsNumber && IsEnumerableOfEnum(type))
            {
                statements.Add("d[\"" + key + "\"] = " + access + " == null ? null : global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(" + access + ", static x => x.ToString()));");
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

        public static ClassModel ParseClass(GeneratorAttributeSyntaxContext context, string memberAttributeName, CancellationToken cancellationToken)
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

            // RULE GEN-3
            if (!hasMembers)
            {
                var attributeName = attribute.AttributeClass?.Name ?? "Attribute";
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN001",
                    "Class '" + symbol.Name + "' is marked [" + attributeName.Replace("Attribute", string.Empty) + "] but has no members decorated with [" + memberAttributeName.Substring(memberAttributeName.LastIndexOf('.') + 1).Replace("Attribute", string.Empty) + "] in its hierarchy.",
                    reference));
            }

            var keepLast = GetInt(attribute, "KeepLast");
            var maxAgeDays = GetInt(attribute, "MaxAgeDays");
            // RULE GEN-6
            if (keepLast < 0 || maxAgeDays < 0)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    "AQGEN004",
                    "Class '" + symbol.Name + "' has a negative KeepLast or MaxAgeDays; use 0 for no limit.",
                    reference));
            }

            return new ClassModel(
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                GetString(attribute, "Name") ?? symbol.Name,
                symbol.NamespaceName(),
                GetBool(attribute, "IncludeId"),
                GetString(attribute, "SnapshotTypeName"),
                keepLast,
                maxAgeDays,
                diagnostics.ToEquatableArray());
        }

        // RULE GEN-3
        private static DiagnosticInfo UnknownMember(SyntaxReference? reference, string attributeName, string option, string memberName, IPropertySymbol property, ITypeSymbol type)
        {
            return DiagnosticInfo.Create(
                "AQGEN003",
                "[" + attributeName.Replace("Attribute", string.Empty) + "] " + option + " member '" + memberName + "' does not exist on type '" + type.ToDisplayString() + "' (property '" + property.Name + "').",
                reference);
        }

        private static bool IsEnumerableOfEnum(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String)
            {
                return false;
            }

            if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return named.TypeArguments[0].TypeKind == TypeKind.Enum;
            }

            return type.AllInterfaces.Any(i =>
                i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                && i.TypeArguments[0].TypeKind == TypeKind.Enum);
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
    }
}
