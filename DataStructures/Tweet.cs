using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures;

public record Tweet
{
    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("text")] public string? Text { get; set; }

    [JsonPropertyName("author")] public Author? Author { get; set; }

    [JsonPropertyName("replies")] public int Replies { get; set; }

    [JsonPropertyName("retweets")] public int Retweets { get; set; }

    [JsonPropertyName("likes")] public int Likes { get; set; }

    [JsonPropertyName("color")] public string? Color { get; set; }

    [JsonPropertyName("twitter_card")] public string? TwitterCard { get; set; }

    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }

    [JsonPropertyName("created_timestamp")]
    public int? CreatedTimestamp { get; set; }

    [JsonPropertyName("possibly_sensitive")]
    public bool? PossiblySensitive { get; set; }

    [JsonPropertyName("views")] public int? Views { get; set; }

    [JsonPropertyName("lang")] public string? Lang { get; set; }

    [JsonPropertyName("replying_to")] public string? ReplyingTo { get; set; }

    [JsonPropertyName("replying_to_status")]
    public string? ReplyingToStatus { get; set; }

    [JsonPropertyName("media")] public FxMedia? Media { get; set; }

    [JsonPropertyName("source")] public string? Source { get; set; }
}