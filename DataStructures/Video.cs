namespace TelegramMediaGrabberBot.DataStructures;

public class Video
{
    public Uri? contentUri { get; set; }
    public Stream? Stream { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
}
