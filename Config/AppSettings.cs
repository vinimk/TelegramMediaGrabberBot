namespace TelegramMediaGrabberBot.Config;

public record AppSettings
{
    public TelegramBotConfig? TelegramBotConfig { get; set; }
    public List<long?>? WhitelistedGroups { get; set; }
    public List<string>? SupportedWebSites { get; set; }
    public List<string>? NitterInstances { get; set; }
    public List<string>? BibliogramInstances { get; set; }
    public int? HoursBetweenBackgroundTask { get; set; }
    public BlueSkyAuth? BlueSkyAuth { get; set; }
}