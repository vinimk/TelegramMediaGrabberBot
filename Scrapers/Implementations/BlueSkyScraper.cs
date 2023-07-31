using CommunityToolkit.Diagnostics;
using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class BlueSkyScraper : ScraperBase
{
    public BlueSkyScraper(IHttpClientFactory httpClientFactory)
    : base(httpClientFactory)
    { }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri postUrl, bool forceDownload = false)
    {
        try
        {
            using HttpClient client = _httpClientFactory.CreateClient("default");
            client.Timeout = new TimeSpan(0, 0, 5);
            HttpResponseMessage response = await client.GetAsync(postUrl.AbsoluteUri);
            HtmlDocument doc = new();
            doc.Load(await response.Content.ReadAsStreamAsync());
            IEnumerable<HtmlNode> metaNodes = doc.DocumentNode.SelectSingleNode("//head").Descendants("meta");

            ScrapedData scraped = new()
            {
                Uri = postUrl
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
                //TO-DO VIDEO NOT AVAILABLE IN BLUESKY YET
                //case "video":
                //    scraped.Type = ScrapedDataType.Media;
                //    Media? media = await YtDownloader.DownloadVideoFromUrlAsync(postUrl.AbsoluteUri, forceDownload);
                //    if (media != null)
                //    {
                //        scraped.Medias = new List<Media>() { media };
                //    }
                //    break;

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
                                    Media imageMedia = new() { Stream = stream, Type = MediaType.Photo };
                                    scraped.Medias.Add(imageMedia);
                                }
                            }
                        }
                        else
                        {
                            scraped.Medias = uriMedias
                                .Select(x => new Media { Uri = x, Type = MediaType.Photo })
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
            return scraped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrap bluesky", postUrl.AbsoluteUri);
        }
        return null;
    }
}
