using System.Text;

namespace AQ.Generators
{
    internal static class Names
    {
        public const string EntityDataRegistry = "global::AQ.Utilities.EntityData.EntityDataRegistry";

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

        public static string HintName(string fullName)
        {
            return fullName.Replace("global::", string.Empty).Replace('<', '_').Replace('>', '_');
        }
    }
}
