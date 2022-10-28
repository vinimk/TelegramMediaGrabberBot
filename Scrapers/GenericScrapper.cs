using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers;

public static class GenericScrapper
{
    public static async Task<ScrapedData?> ExtractContent(Uri uri)
    {
        try
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
        }
        catch { }
        return null;
    }

}
