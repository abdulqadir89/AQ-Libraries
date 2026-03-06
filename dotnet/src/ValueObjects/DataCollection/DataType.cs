using System.Text.Json.Serialization;

namespace AQ.ValueObjects.DataCollection;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataType
{
    Text,
    MultilineText,
    Markdown,
    Numeric,
    Boolean,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    SingleChoice,
    MultiChoice
}

