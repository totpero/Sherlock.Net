using System.Text.Json;
using System.Text.Json.Serialization;
using Sherlock.Net.Core.Serialization;

namespace Sherlock.Net.Core.Models;

public sealed class SiteData
{
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("urlMain")]
    public string UrlMain { get; set; } = string.Empty;

    [JsonPropertyName("errorType")]
    [JsonConverter(typeof(SiteErrorTypeConverter))]
    public SiteErrorType ErrorType { get; set; }

    [JsonPropertyName("errorMsg")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string>? ErrorMsg { get; set; }

    [JsonPropertyName("errorCode")]
    public int? ErrorCode { get; set; }

    [JsonPropertyName("errorUrl")]
    public string? ErrorUrl { get; set; }

    [JsonPropertyName("urlProbe")]
    public string? UrlProbe { get; set; }

    [JsonPropertyName("request_method")]
    public string? RequestMethod { get; set; }

    [JsonPropertyName("request_payload")]
    public JsonElement? RequestPayload { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("regexCheck")]
    public string? RegexCheck { get; set; }

    [JsonPropertyName("username_claimed")]
    public string UsernameClaimed { get; set; } = "blue";

    [JsonPropertyName("isNSFW")]
    public bool IsNsfw { get; set; }

    public string GetProfileUrl(string username)
        => Url.Replace("{}", Uri.EscapeDataString(username));

    public string GetProbeUrl(string username)
        => (UrlProbe ?? Url).Replace("{}", Uri.EscapeDataString(username));
}
