using CommunityToolkit.Diagnostics;
using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class TwitterScraper : ScraperBase
{

    public readonly List<string> _nitterInstances;
    public TwitterScraper(IHttpClientFactory httpClientFactory, List<string> nitterInstances)
    : base(httpClientFactory)
    {
        Guard.IsNotNull(nitterInstances);
        _nitterInstances = nitterInstances;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri twitterUrl, bool forceDownload = false)
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
                    Uri = twitterUrl
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
                        scraped.Type = ScrapedDataType.Media;
                        Media? media = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri, forceDownload);
                        if (media != null)
                        {
                            scraped.Medias = new List<Media>() { media };
                        }
                        break;

                    case "photo":
                        scraped.Type = ScrapedDataType.Media;
                        List<Uri> uriMedias = metaNodes
                         .Where(x => x.GetAttributeValue("property", null) == "og:image" &&
                         !x.GetAttributeValue("content", null).Contains("tw_video_thumb"))
                         .Select(x => x.GetAttributeValue("content", null))
                         .Distinct()
                         .Select(x => new Uri(x, UriKind.Absolute))
                         .ToList();

                        if (uriMedias.Count > 0)
                        {
                            if (forceDownload)
                            {
                                scraped.Medias = new();
                                foreach (Uri? uri in uriMedias)
                                {
                                    Stream? stream = await HttpUtils.GetStreamFromUrl(uri);
                                    if (stream != null)
                                    {
                                        Media imageMedia = new() { Stream = stream, Type = MediaType.Image };
                                        scraped.Medias.Add(imageMedia);
                                    }
                                }
                            }
                            else
                            {
                                scraped.Medias = uriMedias
                                    .Select(x => new Media { Uri = x, Type = MediaType.Image })
                                    .ToList();
                            }
                        }
                        break;
                    case "article":
                        scraped.Type = ScrapedDataType.Text;
                        break;
                    default:
                        break;
                }

                if (scraped.Type == ScrapedDataType.Text && string.IsNullOrWhiteSpace(scraped.Content))
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
        Media? video = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri, forceDownload);
        return video != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = twitterUrl, Medias = new List<Media>() { video } } : null;
    }
}
