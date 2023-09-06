using CommunityToolkit.Diagnostics;
using HtmlAgilityPack;
using System.Net.Http.Json;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;
using Media = TelegramMediaGrabberBot.DataStructures.Medias.Media;

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
        ScrapedData? scrapedData = await ExtractFromFXTwitter(twitterUrl);
        if (scrapedData == null || !scrapedData.IsValid())
        {
            scrapedData = await ExtractFromNitter(twitterUrl, forceDownload);

            if (scrapedData == null || !scrapedData.IsValid())
            {
                MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(twitterUrl.AbsoluteUri, forceDownload);
                return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = twitterUrl, Medias = new List<Media>() { videoObj } } : null;
            }
        }
        return scrapedData;
    }

    public async Task<ScrapedData?> ExtractFromFXTwitter(Uri twitterUrl)
    {
        string host = "api.fxtwitter.com";
        UriBuilder newUriBuilder = new(twitterUrl)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = host,
            Port = -1, //defualt port for schema
        };

        // get a Uri instance from the UriBuilder
        string newUrl = newUriBuilder.Uri.AbsoluteUri.ToString();

        try
        {
            using HttpClient client = _httpClientFactory.CreateClient("default");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TelegramMediaGrabberBot");
            client.Timeout = new TimeSpan(0, 0, 20);

            FxTwitterResponse? response = await client.GetFromJsonAsync<FxTwitterResponse>(newUrl);

            if (response != null &&
                response.Post != null)
            {
                Tweet post = response.Post;
                ScrapedData scraped = new()
                {
                    Uri = twitterUrl,
                    Content = post.Text,
                    Type = ScrapedDataType.Text
                };

                if (post.Author != null)
                {
                    scraped.Author = post.Author.Name;
                }


                if (post.Media != null)
                {
                    if (post.Media.Videos != null)
                    {
                        scraped.Type = ScrapedDataType.Media;

                        foreach (Video video in post.Media.Videos)
                        {
                            if (!string.IsNullOrEmpty(video.Url))
                            {
                                scraped.Medias.Add(new Media { Type = MediaType.Video, Uri = new Uri(video.Url) });
                            }
                        }
                    }

                    if (post.Media.Photos != null)
                    {
                        scraped.Type = ScrapedDataType.Media;

                        foreach (Photo photo in post.Media.Photos)
                        {
                            if (!string.IsNullOrEmpty(photo.Url))
                            {
                                scraped.Medias.Add(new Media { Type = MediaType.Image, Uri = new Uri(photo.Url) });
                            }
                        }
                    }
                }

                return scraped;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed for fxtwitter");
        } //empty catch, if there is any issue with one nitter instance, it will go to the next one
        return null;
    }


    public async Task<ScrapedData?> ExtractFromNitter(Uri twitterUrl, bool forceDownload)
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
                client.Timeout = new TimeSpan(0, 0, 10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/117.0");
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
        return null;
    }
}