using CommunityToolkit.Diagnostics;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Scrapers.Implementations;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers;

public class Scraper
{
    private readonly InstagramScraper _instagramScraper;
    private readonly TwitterScraper _twitterScraper;
    //private readonly BlueSkyScraper _blueSkyScraper;
    private readonly GenericScraper _genericScraper;
    protected readonly ILogger _logger;
    public Scraper(IHttpClientFactory httpClientFactory, AppSettings appSettings)
    {
        Guard.IsNotNull(appSettings);
        Guard.IsNotNull(appSettings.NitterInstances);
        Guard.IsNotNull(appSettings.BibliogramInstances);
        _instagramScraper = new InstagramScraper(httpClientFactory, appSettings.BibliogramInstances);
        _twitterScraper = new TwitterScraper(httpClientFactory, appSettings.NitterInstances);
        //_blueSkyScraper = new BlueSkyScraper(httpClientFactory);
        _genericScraper = new GenericScraper(httpClientFactory);
        _logger = ApplicationLogging.CreateLogger(GetType().Name);
    }

    public async Task<ScrapedData?> GetScrapedDataFromUrlAsync(Uri uri, bool forceDownload = false)
    {
        try
        {
            if (forceDownload == false)
            {
                return uri.AbsoluteUri.Contains("twitter.com")
                    ? await _twitterScraper.ExtractContentAsync(uri)
                    : uri.AbsoluteUri.Contains("x.com")
                    ? await _twitterScraper.ExtractContentAsync(uri)
                    : uri.AbsoluteUri.Contains("instagram.com")
                    ? await _instagramScraper.ExtractContentAsync(uri)
                    //: uri.AbsoluteUri.Contains("bsky.app")
                    //? await _blueSkyScraper.ExtractContentAsync(uri, forceDownload)
                    : await _genericScraper.ExtractContentAsync(uri);
            }
            else
            {
                MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(uri.AbsoluteUri, forceDownload);
                return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = uri, Medias = [videoObj] } : null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("scrapper error {ex}", ex);
            return null;
        }
    }
}