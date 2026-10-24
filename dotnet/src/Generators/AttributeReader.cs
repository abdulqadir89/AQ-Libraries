using System.Linq;
using Microsoft.CodeAnalysis;

namespace AQ.Generators
{
    internal static class AttributeReader
    {
        public static string? GetString(AttributeData attribute, string name)
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

        public static bool GetBool(AttributeData attribute, string name)
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

        public static int GetInt(AttributeData attribute, string name)
        {
            foreach (var pair in attribute.NamedArguments)
            {
                if (pair.Key == name)
                {
                    return pair.Value.Value is int value ? value : 0;
                }
            }

            return 0;
        }

        public static string[] GetStrings(AttributeData attribute, string name)
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
