using System.Text.Json.Serialization;

namespace AQ.ValueObjects.DataCollection;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataType
{
    String,
    Numeric,
    Boolean,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    SingleChoice,
    MultiChoice
}

