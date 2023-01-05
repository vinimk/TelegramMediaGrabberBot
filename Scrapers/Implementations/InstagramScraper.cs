using HtmlAgilityPack;
using System.Collections.Specialized;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class InstagramScraper : ScraperBase
{

    private readonly List<string> _bibliogramInstances;
    public InstagramScraper(IHttpClientFactory httpClientFactory, List<string> bibliogramInstances)
        : base(httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(bibliogramInstances);
        _bibliogramInstances = bibliogramInstances;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri instagramUrl, bool forceDownload = false)
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
                        Uri = instagramUrl,
                        Type = ScrapedDataType.Media
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
                        string videoUrl = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").FirstChild.GetAttributeValue("src", null);

                        if (!videoUrl.StartsWith(bibliogramInstance))
                        {
                            videoUrl = "https://" + bibliogramInstance + videoUrl;
                        }

                        Uri videoUri = new(videoUrl);

                        MediaDetails? video = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri, forceDownload);
                        if (video != null)
                        {
                            scraped.Medias = new List<Media>()
                            {
                                video
                            };
                        }
                    }

                    else if (mediaType.StartsWith("Photo by") ||
                        mediaType.StartsWith("Post by"))
                    {
                        List<HtmlNode> elements = doc.DocumentNode.SelectSingleNode("//section[@class='images-gallery']").ChildNodes
                         .Select(x => x)
                         .Distinct()
                         .ToList();

                        if (elements.Count > 0 &&
                            scraped.Medias != null)
                        {
                            foreach (HtmlNode? element in elements)
                            {
                                string url = element.GetAttributeValue("src", null);
                                if (!url.StartsWith("http"))
                                {
                                    url = "http://" + bibliogramInstance + url;
                                }

                                Uri uri = new(url, UriKind.Absolute);
                                MediaType type = MediaType.Photo;
                                if (element.Name == "video")
                                {
                                    type = MediaType.Video;
                                }

                                Media media;
                                if (forceDownload == true)
                                {
                                    Stream? stream = null;
                                    NameValueCollection queryString = HttpUtility.ParseQueryString(uri.Query);
                                    if (queryString != null)
                                    {
                                        string? directInstagramUrl = queryString["url"];
                                        if (directInstagramUrl != null)
                                        {
                                            stream = await HttpUtils.GetStreamFromUrl(new Uri(directInstagramUrl, UriKind.Absolute));
                                        }
                                    }
                                    media = new() { Stream = stream, Type = type };
                                }
                                else
                                {
                                    media = new() { Uri = uri, Type = type };
                                }
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
        MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri, forceDownload);
        return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = instagramUrl, Medias = new List<Media>() { videoObj } } : null;
    }
}
