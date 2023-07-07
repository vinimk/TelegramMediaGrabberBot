using CommunityToolkit.Diagnostics;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Scrapers.Implementations;

namespace TelegramMediaGrabberBot.Scrapers;

public class Scraper
{
    private readonly InstagramScraper _instagramScraper;
    private readonly TwitterScraper _twitterScraper;
    private readonly GenericScraper _genericScraper;
    public Scraper(IHttpClientFactory httpClientFactory, AppSettings appSettings)
    {
        Guard.IsNotNull(appSettings);
        Guard.IsNotNull(appSettings.NitterInstances);
        Guard.IsNotNull(appSettings.BibliogramInstances);
        _instagramScraper = new InstagramScraper(httpClientFactory, appSettings.BibliogramInstances);
        _twitterScraper = new TwitterScraper(httpClientFactory, appSettings.NitterInstances);
        _genericScraper = new GenericScraper(httpClientFactory);
    }

    public async Task<ScrapedData?> GetScrapedDataFromUrlAsync(Uri uri, bool forceDownload = false)
    {
        return uri.AbsoluteUri.Contains("twitter.com")
            ? await _twitterScraper.ExtractContentAsync(uri, forceDownload)
            : uri.AbsoluteUri.Contains("instagram.com")
            ? await _instagramScraper.ExtractContentAsync(uri, forceDownload)
            : await _genericScraper.ExtractContentAsync(uri, forceDownload);
    }
}
