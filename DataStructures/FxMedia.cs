using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures;

public record FxMedia
{
    [JsonPropertyName("all")] public List<All>? All { get; set; }

    [JsonPropertyName("photos")] public List<Photo>? Photos { get; set; }

    [JsonPropertyName("videos")] public List<Video>? Videos { get; set; }
}