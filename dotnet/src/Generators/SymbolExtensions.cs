using Microsoft.CodeAnalysis;

namespace AQ.Generators
{
    internal static class SymbolExtensions
    {
        public static bool IsInterface(this INamedTypeSymbol type, string ns, string name)
        {
            return type.Name == name && type.TypeArguments.Length == 0 && type.ContainingNamespace.ToDisplayString() == ns;
        }

        public static bool DeclaresMember(this INamedTypeSymbol symbol, string name)
        {
            for (var current = symbol; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                if (current.GetMembers(name).Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static string NamespaceName(this INamedTypeSymbol symbol)
        {
            return symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToDisplayString();
        }
    }
}
