using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures;

public record All
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("width")] public int? Width { get; set; }

    [JsonPropertyName("height")] public int? Height { get; set; }

    [JsonPropertyName("altText")] public string? AltText { get; set; }
}