using CommunityToolkit.Diagnostics;
using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class InstagramScraper : ScraperBase
{
    private readonly string? _userName;
    private readonly string? _password;

    public InstagramScraper(IHttpClientFactory httpClientFactory, List<string> bibliogramInstances, string? userName = null, string? password = null)
        : base(httpClientFactory)
    {
        Guard.IsNotNull(bibliogramInstances);
        _userName = userName;
        _password = password;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri instagramUrl)
    {
        ScrapedData? scrapedData = null;

        for (int i = 0; i < 4; i++) //try 4 times to get because sometimes after the first try ddinstagram gets the file
        {
            scrapedData = await ExtractFromDDInstagram(instagramUrl);
            if(scrapedData != null)
            {
                return scrapedData;
            }
            else
            {
                await Task.Delay(1000);
            }
        }

        if (scrapedData == null || !scrapedData.IsValid())
        {
            MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri, username: _userName, password: _password);
            return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = instagramUrl, Medias = [videoObj] } : null;
        }

        return scrapedData;
    }


    public async Task<ScrapedData?> ExtractFromDDInstagram(Uri instagramUrl)
    {
        string realUrl = await HttpUtils.GetRealUrlFromMoved(instagramUrl.AbsoluteUri);

        string host = "ddinstagram.com";
        UriBuilder newUriBuilder = new(realUrl)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = host,
            Port = -1, //defualt port for schema
        };

        string newUrl = newUriBuilder.Uri.AbsoluteUri;

        try
        {

            using HttpClient client = _httpClientFactory.CreateClient("default");
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Discordbot/2.0; +https://discordapp.com)");


            HttpResponseMessage response = await client.GetAsync(newUrl);
            if (response.IsSuccessStatusCode)
            {
                HtmlDocument doc = new();
                doc.Load(await response.Content.ReadAsStreamAsync());
                IEnumerable<HtmlNode> metaNodes = doc.DocumentNode.SelectSingleNode("//head").Descendants("meta");

                ScrapedData scraped = new()
                {
                    Uri = instagramUrl
                };


                HtmlNode? contentNode = metaNodes.
                    Where(x => x.GetAttributeValue("property", null) == "og:description")
                    .FirstOrDefault();

                if (contentNode != null)
                {
                    scraped.Content = HttpUtility.HtmlDecode(contentNode.GetAttributeValue("content", ""));
                }

                HtmlNode? authorNode = metaNodes.
                    Where(x => x.GetAttributeValue("name", null) == "twitter:title")
                    .FirstOrDefault();

                if (authorNode != null)
                {
                    scraped.Author = HttpUtility.HtmlDecode(authorNode.GetAttributeValue("content", ""));
                }


                HtmlNode? videoNode = metaNodes.Where(x => x.GetAttributeValue("property", null) == "og:video")
                    .FirstOrDefault();

                if (videoNode != null)
                {
                    string videoUrl = videoNode.GetAttributeValue("content", "");
                    scraped.Type = ScrapedDataType.Media;
                    if (!videoUrl.StartsWith("https://"))
                    {
                        videoUrl = $"https://{host}{videoUrl}";
                    }
                    scraped.Medias.Add(new Media { Type = MediaType.Video, Uri = new Uri(videoUrl) });
                }
                else
                {
                    HtmlNode? imageNode = metaNodes.
                        Where(x => x.GetAttributeValue("property", null) == "og:image")
                        .FirstOrDefault();

                    if (imageNode != null)
                    {
                        string imageUrl = imageNode.GetAttributeValue("content", "");
                        scraped.Type = ScrapedDataType.Media;
                        if (!imageUrl.StartsWith("https://"))
                        {
                            imageUrl = $"https://{host}{imageUrl}";
                        }
                        scraped.Medias.Add(new Media { Type = MediaType.Image, Uri = new Uri(imageUrl) });
                    }
                }


                return scraped;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed for DDInstagram");
        } //empty catch, if there is any issue with one nitter instance, it will go to the next one
        return null;
    }


}