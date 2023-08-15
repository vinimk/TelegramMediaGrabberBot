using System.Text.Json.Serialization;

namespace TelegramMediaGrabberBot.DataStructures
{
    public class FxTwitterResponse
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("tweet")]
        public Tweet? Post { get; set; }
    }

    public class All
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        [JsonPropertyName("altText")]
        public string? AltText { get; set; }
    }

    public class Author
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("screen_name")]
        public string? ScreenName { get; set; }
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
        [JsonPropertyName("avatar_color")]
        public string? AvatarColor { get; set; }
        [JsonPropertyName("banner_url")]
        public string? BannerUrl { get; set; }
    }

    public class FxMedia
    {
        [JsonPropertyName("all")]
        public List<All>? All { get; set; }
        [JsonPropertyName("photos")]
        public List<Photo>? Photos { get; set; }
        [JsonPropertyName("videos")]
        public List<Video>? Videos { get; set; }
    }

    public class Video
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        [JsonPropertyName("format")]
        public string? Format { get; set; }
    }


    public class Photo
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("width")]
        public int Width { get; set; }
        [JsonPropertyName("height")]
        public int Height { get; set; }
        [JsonPropertyName("altText")]
        public string? AltText { get; set; }
    }



    public class Tweet
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("author")]
        public Author? Author { get; set; }
        [JsonPropertyName("replies")]
        public int Replies { get; set; }
        [JsonPropertyName("retweets")]
        public int Retweets { get; set; }
        [JsonPropertyName("likes")]
        public int Likes { get; set; }
        [JsonPropertyName("color")]
        public string? Color { get; set; }
        [JsonPropertyName("twitter_card")]
        public string? TwitterCard { get; set; }
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
        [JsonPropertyName("created_timestamp")]
        public int? CreatedTimestamp { get; set; }
        [JsonPropertyName("possibly_sensitive")]
        public bool? PossiblySensitive { get; set; }
        [JsonPropertyName("views")]
        public int? Views { get; set; }
        [JsonPropertyName("lang")]
        public string? Lang { get; set; }
        [JsonPropertyName("replying_to")]
        public string? ReplyingTo { get; set; }
        [JsonPropertyName("replying_to_status")]
        public string? ReplyingToStatus { get; set; }
        [JsonPropertyName("media")]
        public FxMedia? Media { get; set; }
        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }
}