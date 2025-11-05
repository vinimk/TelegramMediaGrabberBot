using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures;

public record Author
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("screen_name")] public string? ScreenName { get; set; }

    [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }

    [JsonPropertyName("avatar_color")] public string? AvatarColor { get; set; }

    [JsonPropertyName("banner_url")] public string? BannerUrl { get; set; }
}