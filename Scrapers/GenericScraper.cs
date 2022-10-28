using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers;

public class GenericScraper : ScraperBase
{
    public GenericScraper(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri uri)
    {
        var urlRequest = await HttpUtils.GetRealUrlFromMoved(uri.AbsoluteUri);
        var video = await YtDownloader.DownloadVideoFromUrlAsync(urlRequest);
        if (video != null)
        {
            ScrapedData scraped = new()
            {
                Url = urlRequest,
                Type = ScrapedDataType.Video,
                Video = video,
                Content = video.Content,
                Author = video.Author
            };
            return scraped;
        }
        return null;
    }

}
