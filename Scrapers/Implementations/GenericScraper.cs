using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class GenericScraper : ScraperBase
{
    public GenericScraper(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri uri, bool forceDownload = false)
    {
        string urlRequest = await HttpUtils.GetRealUrlFromMoved(uri.AbsoluteUri);
        MediaDetails? media = await YtDownloader.DownloadVideoFromUrlAsync(urlRequest, forceDownload);
        if (media != null)
        {
            ScrapedData scraped = new()
            {
                Uri = new Uri(urlRequest, UriKind.Absolute),
                Medias = new() { media },
                Content = media.Content,
                Author = media.Author,
                Type = ScrapedDataType.Media
            };
            return scraped;
        }
        return null;
    }
}