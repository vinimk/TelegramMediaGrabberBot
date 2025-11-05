namespace TelegramMediaGrabberBot.DataStructures.Medias;

public record MediaDetails : Media
{
    public string? Content { get; set; }
    public string? Author { get; set; }
}