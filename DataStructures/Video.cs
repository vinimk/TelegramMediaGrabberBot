namespace TelegramMediaGrabberBot.DataStructures;

public class Video
{
    public Uri? contentUri { get; set; }
    public Stream? Stream { get; set; }
    public String? Content { get; set; }
    public String? Author { get; set; }
}
