using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoachPulse.Application.DTOs.Coach;

public sealed class FlexibleRepsConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => reader.GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new JsonException("Reps must be a number or text range.")
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
