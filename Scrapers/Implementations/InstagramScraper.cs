﻿using CommunityToolkit.Diagnostics;
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
    private readonly List<string> _instagramProxies;

    public InstagramScraper(IHttpClientFactory httpClientFactory, List<string> instagramProxies, string? userName = null, string? password = null)
        : base(httpClientFactory)
    {
        Guard.IsNotNull(instagramProxies);
        _instagramProxies = instagramProxies;
        _userName = userName;
        _password = password;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri instagramUrl, bool forceDownload = false)
    {
        ScrapedData? scrapedData = null;

        for (int i = 0; i < 3; i++) //3 times for each
        {
            foreach (string hostUrl in _instagramProxies)
            {
                scrapedData = await ExtractFromMetaInstagram(hostUrl, instagramUrl);
                if (scrapedData != null)
                {
                    return scrapedData;
                }
                else
                {
                    await Task.Delay(2000);
                }
            }
        }

        if (scrapedData == null || !scrapedData.IsValid())
        {
            MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri, username: _userName, password: _password);
            return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = instagramUrl, Medias = [videoObj] } : null;
        }

        return scrapedData;
    }


    public async Task<ScrapedData?> ExtractFromMetaInstagram(string hostUrl, Uri instagramUrl)
    {
        UriBuilder newUriBuilder = new(instagramUrl.AbsoluteUri)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = hostUrl,
            Port = -1, //defualt port for schema
        };

        string newUrl = newUriBuilder.Uri.AbsoluteUri;

        try
        {

            using HttpClient client = _httpClientFactory.CreateClient("default");
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("discordbot");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            HttpResponseMessage response = await client.GetAsync(newUrl);

            _logger.LogInformation("url {url}", newUrl);
            _logger.LogInformation("response {response}", response.ToString());

            if (response.IsSuccessStatusCode)
            {
                HtmlDocument doc = new();
                doc.Load(await response.Content.ReadAsStreamAsync());
                IEnumerable<HtmlNode> metaNodes = doc.DocumentNode.SelectSingleNode("//head")!.Descendants("meta");

                ScrapedData scraped = new()
                {
                    Uri = instagramUrl
                };


                HtmlNode? contentNode = metaNodes.
                    Where(x => x.GetAttributeValue("property", string.Empty) == "og:description")
                    .FirstOrDefault();

                if (contentNode != null)
                {
                    scraped.Content = HttpUtility.HtmlDecode(contentNode.GetAttributeValue("content", ""));
                }

                HtmlNode? authorNode = metaNodes.
                    Where(x => x.GetAttributeValue("name", string.Empty) == "twitter:title")
                    .FirstOrDefault();

                if (authorNode != null)
                {
                    scraped.Author = HttpUtility.HtmlDecode(authorNode.GetAttributeValue("content", ""));
                }


                HtmlNode? videoNode = metaNodes
                    .Where(x => x.GetAttributeValue("property", string.Empty) is "og:video" or "og:video:url")
                    .FirstOrDefault();

                if (videoNode != null)
                {
                    string videoUrl = videoNode.GetAttributeValue("content", "");
                    scraped.Type = ScrapedDataType.Media;
                    if (!videoUrl.StartsWith("https://"))
                    {
                        videoUrl = $"https://{hostUrl}{videoUrl}";
                    }
                    scraped.Medias!.Add(new Media { Type = MediaType.Video, Uri = new Uri(videoUrl) });
                }
                else
                {
                    HtmlNode? imageNode = metaNodes
                        .Where(x => x.GetAttributeValue("property", string.Empty) is "og:image" or "og:image:url")
                        .FirstOrDefault();

                    if (imageNode != null)
                    {
                        string imageUrl = imageNode.GetAttributeValue("content", "");
                        scraped.Type = ScrapedDataType.Media;
                        if (!imageUrl.StartsWith("https://"))
                        {
                            imageUrl = $"https://{hostUrl}{imageUrl}";
                        }
                        scraped.Medias!.Add(new Media { Type = MediaType.Image, Uri = new Uri(imageUrl) });
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