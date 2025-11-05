using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures;

public record FxTwitterResponse
{
    [JsonPropertyName("code")] public int? Code { get; set; }

    [JsonPropertyName("message")] public string? Message { get; set; }

    [JsonPropertyName("tweet")] public Tweet? Post { get; set; }
}