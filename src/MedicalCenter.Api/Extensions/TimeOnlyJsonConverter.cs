using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicalCenter.Api.Extensions;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (TimeOnly.TryParseExact(value, "HH:mm", out var result) ||
            TimeOnly.TryParseExact(value, "HH:mm:ss", out result) ||
            TimeOnly.TryParse(value, out result))
        {
            return result;
        }

        throw new JsonException($"Cannot convert \"{value}\" to TimeOnly.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("HH:mm"));
    }
}
