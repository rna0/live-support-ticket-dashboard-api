using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveSupportDashboard.Domain.Enums;

public class JsonStringEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            throw new JsonException($"Cannot convert null or empty string to {typeof(TEnum).Name}");
        }

        return Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out var result)
            ? result
            : throw new JsonException($"Unable to convert \"{stringValue}\" to {typeof(TEnum).Name}");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
