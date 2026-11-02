using System.Globalization;

namespace AQ.Utilities.Concurrency;

public enum IfMatchKind
{
    Missing,
    Any,
    Revision,
    Invalid,
}

public readonly record struct IfMatch(IfMatchKind Kind, int Revision = 0);

public static class EntityTag
{
    // RULE CON-1
    public static string Format(int revision) => $"\"{revision.ToString(CultureInfo.InvariantCulture)}\"";

    // RULE CON-2
    public static IfMatch Parse(string? headerValue)
    {
        var value = headerValue?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return new IfMatch(IfMatchKind.Missing);
        }

        if (value == "*")
        {
            return new IfMatch(IfMatchKind.Any);
        }

        if (value.Length > 2 && value[0] == '"' && value[^1] == '"'
            && int.TryParse(value.AsSpan(1, value.Length - 2), NumberStyles.None, CultureInfo.InvariantCulture, out var revision))
        {
            return new IfMatch(IfMatchKind.Revision, revision);
        }

        return new IfMatch(IfMatchKind.Invalid);
    }
}
