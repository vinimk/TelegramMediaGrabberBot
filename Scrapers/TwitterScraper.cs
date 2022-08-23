using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers
{
    public static class TwitterScraper
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("TwitterScraper");
        public static readonly List<string?> NitterInstances;
        static TwitterScraper() => NitterInstances = new();


        public static async Task<ScrapedData?> ExtractContent(Uri twitterUrl)
        {
            if (NitterInstances != null)
            {
                foreach (var nitterInstance in NitterInstances)
                {
                    try
                    {
                        var newUriBuilder = new UriBuilder(twitterUrl)
                        {
                            Host = nitterInstance
                        };

                        // get a Uri instance from the UriBuilder
                        var newUri = newUriBuilder.Uri;


                        using HttpClient client = new();
                        var response = await client.GetAsync(newUri.AbsoluteUri, HttpCompletionOption.ResponseHeadersRead);
                        var doc = new HtmlDocument();
                        doc.Load(await response.Content.ReadAsStreamAsync());
                        var metaNodes = doc.DocumentNode.SelectSingleNode("//head").Descendants("meta");


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
                                var videoStream = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri);
                                scraped.VideoStream = videoStream;
                                break;

                            case "photo":
                                scraped.Type = DataStructures.ScrapedDataType.Photo;
                                var imageStrings = metaNodes
                                 .Where(x => x.GetAttributeValue("property", null) == "og:image" &&
                                 !x.GetAttributeValue("content", null).Contains("tw_video_thumb"))
                                 .Select(x => x.GetAttributeValue("content", null))
                                 .Distinct()
                                 .ToList();

                                if (imageStrings.Count > 0)
                                {
                                    scraped.ImagesUrl = imageStrings;
                                }
                                break;
                            case "article":
                                scraped.Type = DataStructures.ScrapedDataType.Article;
                                break;
                            default:
                                break;
                        }
                        return scraped;
                    }
                    catch { }//empty catch, if there is any issue with one nitter instance, it will go to the next one
                }
            }
            return null;
        }
    }
}
