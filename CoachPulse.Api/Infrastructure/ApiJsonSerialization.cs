using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoachPulse.Api.Infrastructure;

public static class ApiJsonSerialization
{
    public static void AddStringEnums(JsonSerializerOptions options)
    {
        if (!options.Converters.Any(converter => converter is JsonStringEnumConverter))
            options.Converters.Add(new JsonStringEnumConverter());
    }
}
