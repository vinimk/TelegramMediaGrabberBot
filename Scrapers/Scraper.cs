using CommunityToolkit.Diagnostics;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Scrapers.Implementations;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers;

public class Scraper
{
    private readonly InstagramScraper? _instagramScraper;
    private readonly TwitterScraper? _twitterScraper;
    private readonly BlueSkyScraper? _blueSkyScraper;
    private readonly GenericScraper _genericScraper;
    protected readonly ILogger _logger;
    public Scraper(IHttpClientFactory httpClientFactory, AppSettings appSettings)
    {
        Guard.IsNotNull(appSettings);
        Guard.IsNotNull(appSettings.NitterInstances);
        Guard.IsNotNull(appSettings.BibliogramInstances);
        _twitterScraper = new TwitterScraper(httpClientFactory, appSettings.NitterInstances);

        if (appSettings.InstagramAuth != null)
        {
            Guard.IsNotNullOrWhiteSpace(appSettings.InstagramAuth.UserName);
            Guard.IsNotNullOrWhiteSpace(appSettings.InstagramAuth.Password);
            _instagramScraper = new InstagramScraper(httpClientFactory, appSettings.BibliogramInstances, userName: appSettings.InstagramAuth.UserName, password: appSettings.InstagramAuth.Password);
        }
        else
        {
            _instagramScraper = new InstagramScraper(httpClientFactory, appSettings.BibliogramInstances);
        }

        if (appSettings.BlueSkyAuth != null)
        {
            Guard.IsNotNullOrWhiteSpace(appSettings.BlueSkyAuth.UserName);
            Guard.IsNotNullOrWhiteSpace(appSettings.BlueSkyAuth.Password);
            _blueSkyScraper = new BlueSkyScraper(httpClientFactory, appSettings.BlueSkyAuth.UserName, appSettings.BlueSkyAuth.Password);
        }

        _genericScraper = new GenericScraper(httpClientFactory);
        _logger = ApplicationLogging.CreateLogger(GetType().Name);
    }

    public async Task<ScrapedData?> GetScrapedDataFromUrlAsync(Uri uri, bool forceDownload = false)
    {
        if (forceDownload == false)
        {
            //removes www. from the host
            string host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;

            return host switch
            {
                "twitter.com" or "fxtwitter.com" or "x.com" => await _twitterScraper!.ExtractContentAsync(uri),
                "bsky.app" => await _blueSkyScraper!.ExtractContentAsync(uri),
                "instagram.com" or "ddinstagram.com" => await _instagramScraper!.ExtractContentAsync(uri),
                "tiktok.com" or "vm.tiktok.com" => await _genericScraper.ExtractContentAsync(uri, true),

                _ => await _genericScraper.ExtractContentAsync(uri),
            };
        }
        else
        {
            MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(uri.AbsoluteUri, forceDownload);
            return videoObj != null ? new ScrapedData { Author = videoObj.Author, Content = videoObj.Content, Type = ScrapedDataType.Media, Uri = uri, Medias = [videoObj] } : null;
        }
    }
}