using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot
{
    public static class TwitterImageScrapper
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("TwitterImageScrapper");
        public static readonly List<string?> NitterInstances;
        static TwitterImageScrapper() => NitterInstances = new();


        public static async Task<Tweet?> ExtractTweetContent(Uri twitterUrl)
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


                        Tweet tweet = new()
                        {
                            Url = twitterUrl.AbsoluteUri
                        };

                        string tweetContet = HttpUtility.HtmlDecode(metaNodes.
                            Where(x => x.GetAttributeValue("property", null) == "og:description")
                            .First()
                            .GetAttributeValue("content", ""));

                        tweet.Content = tweetContet;

                        string tweetAuthor = HttpUtility.HtmlDecode(metaNodes.
                            Where(x => x.GetAttributeValue("property", null) == "og:title")
                            .First()
                            .GetAttributeValue("content", ""));

                        tweet.Author = tweetAuthor;

                        string tweetType = HttpUtility.HtmlDecode(metaNodes.
                            Where(x => x.GetAttributeValue("property", null) == "og:type")
                            .First()
                            .GetAttributeValue("content", ""));

                        tweet.SetType(tweetType);
                        switch (tweet.Type)
                        {
                            case TweetType.Video:
                                var videoStream = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri);
                                tweet.VideoStream = videoStream;
                                return tweet;

                            case TweetType.Photo:
                                var imageStrings = metaNodes
                                 .Where(x => x.GetAttributeValue("property", null) == "og:image" &&
                                 !x.GetAttributeValue("content", null).Contains("tw_video_thumb"))
                                 .Select(x => x.GetAttributeValue("content", null))
                                 .Distinct()
                                 .ToList();

                                if (imageStrings.Count > 0)
                                {
                                    tweet.ImagesUrl = imageStrings;
                                    return tweet;
                                }
                                break;

                            case TweetType.Article:
                            default:
                                return tweet;
                        }
                    }
                    catch { }//empty catch, if there is any issue with one nitter instance, it will go to the next one
                }
            }
            return null;
        }
    }
}
