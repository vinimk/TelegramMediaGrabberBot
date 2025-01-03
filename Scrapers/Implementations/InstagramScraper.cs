﻿using CommunityToolkit.Diagnostics;
using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class InstagramScraper : ScraperBase
{

    private readonly List<string> _bibliogramInstances;
    private readonly string? _userName;
    private readonly string? _password;

    public InstagramScraper(IHttpClientFactory httpClientFactory, List<string> bibliogramInstances, string? userName = null, string? password = null)
        : base(httpClientFactory)
    {
        Guard.IsNotNull(bibliogramInstances);
        _bibliogramInstances = bibliogramInstances;
        _userName = userName;
        _password = password;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri instagramUrl)
    {
        ScrapedData? scrapedData = await ExtractFromDDInstagram(instagramUrl);
        if (scrapedData == null || !scrapedData.IsValid())
        {
            scrapedData = await ExtractFromBibliogram(instagramUrl);

            if (scrapedData == null || !scrapedData.IsValid())
            {
                MediaDetails? videoObj = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri, username: _userName, password: _password);
                return videoObj != null ? new ScrapedData { Type = ScrapedDataType.Media, Uri = instagramUrl, Medias = [videoObj] } : null;
            }
        }
        return scrapedData;
    }


    public async Task<ScrapedData?> ExtractFromDDInstagram(Uri instagramUrl)
    {
        string host = "ddinstagram.com";
        UriBuilder newUriBuilder = new(instagramUrl)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = host,
            Port = -1, //defualt port for schema
        };

        // get a Uri instance from the UriBuilder
        string newUrl = await HttpUtils.GetRealUrlFromMoved(newUriBuilder.Uri.AbsoluteUri);
        if (newUrl == instagramUrl.AbsoluteUri)
        {
            return null;
        }

        try
        {

            using HttpClient client = _httpClientFactory.CreateClient("default");
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("curl");


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
                        imageUrl = $"https://{host}{imageUrl}";
                        scraped.Medias.Add(new Media { Type = MediaType.Image, Uri = new Uri(imageUrl) });
                    }
                }


                return scraped;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed for DDInstagram");
        } //empty catch, if there is any issue with one nitter instance, it will go to the next one
        return null;
    }

    public async Task<ScrapedData?> ExtractFromBibliogram(Uri instagramUrl)
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

                        MediaDetails? video = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
                        if (video != null)
                        {
                            scraped.Medias.Add(video);
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
                                MediaType type = MediaType.Image;
                                if (element.Name == "video")
                                {
                                    type = MediaType.Video;
                                }

                                Media media = new() { Uri = uri, Type = type };

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
        return null;
    }
}