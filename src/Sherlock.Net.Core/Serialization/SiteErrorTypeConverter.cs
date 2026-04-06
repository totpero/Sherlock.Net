using System.Text.Json;
using System.Text.Json.Serialization;
using Sherlock.Net.Core.Models;

namespace Sherlock.Net.Core.Serialization;

public sealed class SiteErrorTypeConverter : JsonConverter<SiteErrorType>
{
    public override SiteErrorType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return SiteErrorType.StatusCode;

        var value = reader.GetString();
        return value switch
        {
            "status_code" => SiteErrorType.StatusCode,
            "message" => SiteErrorType.Message,
            "response_url" => SiteErrorType.ResponseUrl,
            _ => SiteErrorType.StatusCode // Fallback for unknown types
        };
    }

    public override void Write(Utf8JsonWriter writer, SiteErrorType value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            SiteErrorType.StatusCode => "status_code",
            SiteErrorType.Message => "message",
            SiteErrorType.ResponseUrl => "response_url",
            _ => throw new JsonException($"Unknown error type: {value}")
        };
        writer.WriteStringValue(str);
    }
}
