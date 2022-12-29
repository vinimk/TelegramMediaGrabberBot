using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using static TelegramMediaGrabberBot.DataStructures.ScrapedData;

namespace TelegramMediaGrabberBot.Scrapers;

public class TwitterScraper : ScraperBase
{

    public readonly List<string> _nitterInstances;
    public TwitterScraper(IHttpClientFactory httpClientFactory, List<string> nitterInstances)
    : base(httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(nitterInstances);
        _nitterInstances = nitterInstances;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri twitterUrl)
    {

        foreach (string nitterInstance in _nitterInstances)
        {
            UriBuilder newUriBuilder = new(twitterUrl)
            {
                Host = nitterInstance
            };

            // get a Uri instance from the UriBuilder
            Uri newUri = newUriBuilder.Uri;
            try
            {
                using HttpClient client = _httpClientFactory.CreateClient("default");
                client.Timeout = new TimeSpan(0, 0, 5);
                HttpResponseMessage response = await client.GetAsync(newUri.AbsoluteUri);
                HtmlDocument doc = new();
                doc.Load(await response.Content.ReadAsStreamAsync());
                IEnumerable<HtmlNode> metaNodes = doc.DocumentNode.SelectSingleNode("//head").Descendants("meta");


                ScrapedData scraped = new()
                {
                    Url = twitterUrl.AbsoluteUri
                };

                string tweetContet = HttpUtility.HtmlDecode(metaNodes.
                    Where(x => x.GetAttributeValue("property", null) == "og:description")
                    .First()
                    .GetAttributeValue("content", ""));

                scraped.Content = tweetContet;

                string tweetAuthor = HttpUtility.HtmlDecode(metaNodes.
                    Where(x => x.GetAttributeValue("property", null) == "og:title")
                    .First()
                    .GetAttributeValue("content", ""));

                scraped.Author = tweetAuthor;

                string tweetType = HttpUtility.HtmlDecode(metaNodes.
                    Where(x => x.GetAttributeValue("property", null) == "og:type")
                    .First()
                    .GetAttributeValue("content", ""));

                switch (tweetType)
                {
                    case "video":
                        scraped.Type = DataStructures.ScrapedDataType.Video;
                        Video? videoStream = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri);
                        scraped.Video = videoStream;
                        break;

                    case "photo":
                        scraped.Type = DataStructures.ScrapedDataType.Photo;
                        var imageMedias = metaNodes
                         .Where(x => x.GetAttributeValue("property", null) == "og:image" &&
                         !x.GetAttributeValue("content", null).Contains("tw_video_thumb"))
                         .Select(x => new Media() { Url = x.GetAttributeValue("content", null), Type = ScrapedDataType.Photo })
                         .Distinct()
                         .ToList();
                        if (imageMedias.Count > 0)
                        {
                            scraped.Medias = imageMedias;
                        }
                        break;
                    case "article":
                        scraped.Type = DataStructures.ScrapedDataType.Article;
                        break;
                    default:
                        break;
                }

                if (scraped.Type == ScrapedDataType.Article && string.IsNullOrWhiteSpace(scraped.Content))
                {
                    //if the content is empty and the type is an article, the tweet was not scrapped right so we ignore it
                    continue;
                }

                return scraped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed for nitter instance {instance}", newUri.AbsoluteUri);
            }//empty catch, if there is any issue with one nitter instance, it will go to the next one
        }

        //as a last effort if everything fails, try direct download from yt-dlp
        Video? video = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri);
        return video != null ? new ScrapedData { Type = ScrapedDataType.Video, Url = twitterUrl.AbsoluteUri, Video = video } : null;
    }
}
