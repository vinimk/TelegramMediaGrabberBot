using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers;

public class Scraper
{
    private readonly InstagramScraper _instagramScraper;
    private readonly TwitterScraper _twitterScraper;
    private readonly GenericScraper _genericScraper;
    public Scraper(IHttpClientFactory httpClientFactory, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings.NitterInstances);
        ArgumentNullException.ThrowIfNull(appSettings.BibliogramInstances);
        _instagramScraper = new InstagramScraper(httpClientFactory, appSettings.BibliogramInstances);
        _twitterScraper = new TwitterScraper(httpClientFactory, appSettings.NitterInstances);
        _genericScraper = new GenericScraper(httpClientFactory);
    }

    public async Task<ScrapedData?> GetScrapedDataFromUrlAsync(Uri uri)
    {
        return uri.AbsoluteUri.Contains("twitter.com")
            ? await _twitterScraper.ExtractContentAsync(uri)
            : uri.AbsoluteUri.Contains("instagram.com")
            ? await _instagramScraper.ExtractContentAsync(uri)
            : await _genericScraper.ExtractContentAsync(uri);
    }
}
