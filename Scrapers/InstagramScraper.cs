using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using static TelegramMediaGrabberBot.DataStructures.ScrapedData;

namespace TelegramMediaGrabberBot.Scrapers;

public class InstagramScraper : ScraperBase
{

    private readonly List<string> _bibliogramInstances;
    public InstagramScraper(IHttpClientFactory httpClientFactory, List<string> bibliogramInstances)
        : base(httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(bibliogramInstances);
        _bibliogramInstances = bibliogramInstances;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri instagramUrl)
    {
        foreach (string bibliogramInstance in _bibliogramInstances)
        {

            UriBuilder newUriBuilder = new(instagramUrl)
            {
                Host = bibliogramInstance
            };

            // get a Uri instance from the UriBuilder
            Uri newUri = newUriBuilder.Uri;

            try
            {
                using HttpClient client = _httpClientFactory.CreateClient("default");
                client.Timeout = new TimeSpan(0, 0, 5);
                HttpResponseMessage response = await client.GetAsync(newUri.AbsoluteUri);
                if (response.IsSuccessStatusCode)
                {
                    HtmlDocument doc = new();
                    doc.Load(await response.Content.ReadAsStreamAsync());

                    ScrapedData scraped = new()
                    {
                        Url = instagramUrl.AbsoluteUri
                    };

                    HtmlNode nodeContent = doc.DocumentNode.SelectSingleNode("//p[@class='structured-text description']");
                    if (nodeContent != null)
                    {
                        string content = HttpUtility.HtmlDecode(nodeContent.InnerText);

                        scraped.Content = content;
                    }

                    string author = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//a[@class='name']").InnerText);

                    scraped.Author = author;

                    string mediaType = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", null));

                    if (mediaType.StartsWith("Video by"))
                    {
                        scraped.Type = DataStructures.ScrapedDataType.Video;
                        string videoUrl = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").FirstChild.GetAttributeValue("src", null);

                        if (!videoUrl.StartsWith(bibliogramInstance))
                        {
                            videoUrl = "https://" + bibliogramInstance + videoUrl;
                        }

                        Uri videoUri = new(videoUrl);

                        Video? video = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);

                        scraped.Video = video;
                    }

                    else if (mediaType.StartsWith("Photo by") ||
                        mediaType.StartsWith("Post by"))
                    {
                        scraped.Type = DataStructures.ScrapedDataType.Photo;
                        var elements = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").ChildNodes
                         .Select(x => x)
                         .Distinct()
                         .ToList();

                        if (elements.Count > 0 &&
                            scraped.Medias != null)
                        {
                            foreach (var element in elements)
                            {
                                var url = element.GetAttributeValue("src", null);
                                if (!url.StartsWith("http"))
                                {
                                    url = bibliogramInstance + url;
                                }

                                ScrapedDataType type = ScrapedDataType.Photo;

                                if (element.Name == "video")
                                {
                                    type = ScrapedDataType.Video;
                                }
                                var media = new Media() { Url = url, Type = type };
                                scraped.Medias.Add(media);
                            }
                        }
                    }
                    return scraped;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed for bibliogram instance {instance}", newUri.AbsoluteUri);
            }//empty catch, if there is any issue with one nitter instance, it will go to the next one
        }

        //as a last effort if everything fails, try direct download from yt-dlp
        Video? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
        return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Video, Url = instagramUrl.AbsoluteUri, Video = videoObj } : null;
    }
}
