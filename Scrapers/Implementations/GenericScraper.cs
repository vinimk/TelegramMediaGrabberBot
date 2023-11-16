using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class GenericScraper(IHttpClientFactory httpClientFactory) : ScraperBase(httpClientFactory)
{
    public override async Task<ScrapedData?> ExtractContentAsync(Uri uri)
    {
        string urlRequest = await HttpUtils.GetRealUrlFromMoved(uri.AbsoluteUri);
        MediaDetails? media = await YtDownloader.DownloadVideoFromUrlAsync(urlRequest, false);
        if (media != null)
        {
            ScrapedData scraped = new()
            {
                Uri = new Uri(urlRequest, UriKind.Absolute),
                Medias = [media],
                Content = media.Content,
                Author = media.Author,
                Type = ScrapedDataType.Media
            };
            return scraped;
        }
        return null;
    }
}