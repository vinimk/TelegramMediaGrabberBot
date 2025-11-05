using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures;

public record Video
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("thumbnail_url")] public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("width")] public int? Width { get; set; }

    [JsonPropertyName("height")] public int? Height { get; set; }

    [JsonPropertyName("format")] public string? Format { get; set; }
}