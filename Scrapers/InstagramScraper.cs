using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers;

public class InstagramScraper : ScraperBase
{

    private readonly List<string?> _bibliogramInstances;
    public InstagramScraper(IHttpClientFactory httpClientFactory, List<String?> bibliogramInstances)
        : base(httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(bibliogramInstances);
        _bibliogramInstances = bibliogramInstances;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri instagramUrl)
    {
        foreach (var bibliogramInstance in _bibliogramInstances)
        {
            try
            {
                var newUriBuilder = new UriBuilder(instagramUrl)
                {
                    Host = bibliogramInstance
                };

                // get a Uri instance from the UriBuilder
                var newUri = newUriBuilder.Uri;


                using HttpClient client = _httpClientFactory.CreateClient();
                client.Timeout = new TimeSpan(0, 0, 5);
                var response = await client.GetAsync(newUri.AbsoluteUri);
                if (response.IsSuccessStatusCode)
                {
                    var doc = new HtmlDocument();
                    doc.Load(await response.Content.ReadAsStreamAsync());

                    ScrapedData scraped = new()
                    {
                        Url = instagramUrl.AbsoluteUri
                    };

                    var nodeContent = doc.DocumentNode.SelectSingleNode("//p[@class='structured-text description']");
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
                        var videoUrl = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").FirstChild.GetAttributeValue("src", null);

                        if (!videoUrl.StartsWith("http"))
                        {
                            videoUrl = bibliogramInstance + videoUrl;
                        }

                        var videoStream = await YtDownloader.DownloadVideoFromUrlAsync(videoUrl);
                        if (videoStream == null) //if video download fails from bibliogram, download from instagram instead
                        {
                            _logger.LogError("Failed to download video from mirror {bibliogramInstance}, trying original instagram", bibliogramInstance);
                            videoStream = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
                            if (videoStream == null) //if it also fails from instagram, try the other bibliogram instance
                            {
                                continue;
                            }
                        }
                        scraped.Video = videoStream;
                    }
                    else if (mediaType.StartsWith("Photo by") ||
                        mediaType.StartsWith("Post by"))
                    {
                        scraped.Type = DataStructures.ScrapedDataType.Photo;
                        var imageUrls = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").ChildNodes
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
            catch { }
        }

        //as a last effort if everything fails, try direct download from yt-dlp
        var video = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
        return new ScrapedData { Type = ScrapedDataType.Video, Url = instagramUrl.AbsoluteUri, Video = video };
    }
}
