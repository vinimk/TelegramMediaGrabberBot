namespace TelegramMediaGrabberBot.DataStructures.Medias;

public record Media
{
    public Stream? Stream { get; set; }
    public Uri? Uri { get; set; }
    public MediaType? Type { get; set; }
}