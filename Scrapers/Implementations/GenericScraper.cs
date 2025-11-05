using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class GenericScraper(IHttpClientFactory httpClientFactory) : ScraperBase(httpClientFactory)
{
    public override async Task<ScrapedData?> ExtractContentAsync(Uri uri, bool forceDownload = false)
    {
        var media = await YtDownloader.DownloadVideoFromUrlAsync(uri.AbsoluteUri, forceDownload);
        if (media != null)
        {
            ScrapedData scraped = new()
            {
                Uri = uri,
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