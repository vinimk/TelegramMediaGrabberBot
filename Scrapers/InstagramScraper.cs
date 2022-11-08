using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;

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
            try
            {
                UriBuilder newUriBuilder = new(instagramUrl)
                {
                    Host = bibliogramInstance
                };

                // get a Uri instance from the UriBuilder
                Uri newUri = newUriBuilder.Uri;


                using HttpClient client = _httpClientFactory.CreateClient();
                client.Timeout = new TimeSpan(0, 0, 30);
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
                        List<string> imageUrls = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").ChildNodes
                         .Select(x => x.GetAttributeValue("src", null))
                         .Distinct()
                         .ToList();

                        if (imageUrls.Count > 0 &&
                            scraped.ImagesUrl != null)
                        {
                            foreach (string imgUrl in imageUrls)
                            {
                                if (!imgUrl.StartsWith("http"))
                                {
                                    scraped.ImagesUrl.Add(bibliogramInstance + imgUrl);
                                }
                                else
                                {
                                    scraped.ImagesUrl.Add(imgUrl);
                                }
                            }
                        }
                    }
                    return scraped;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed for bibliogram instance {instance}", bibliogramInstance);
            }//empty catch, if there is any issue with one nitter instance, it will go to the next one
        }

        //as a last effort if everything fails, try direct download from yt-dlp
        Video? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
        return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Video, Url = instagramUrl.AbsoluteUri, Video = videoObj } : null;
    }
}
