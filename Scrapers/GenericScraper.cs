using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers;

public class GenericScraper : ScraperBase
{
    public GenericScraper(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) { }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri uri)
    {
        string urlRequest = await HttpUtils.GetRealUrlFromMoved(uri.AbsoluteUri);
        Video? video = await YtDownloader.DownloadVideoFromUrlAsync(urlRequest);
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
