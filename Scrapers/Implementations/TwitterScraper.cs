﻿using CommunityToolkit.Diagnostics;
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

    public override async Task<ScrapedData?> ExtractContentAsync(Uri url)
    {
        ScrapedData? scrapedData = await ExtractFromFXTwitter(url);
        //if (scrapedData == null || !scrapedData.IsValid())
        //{
        //    scrapedData = await ExtractFromNitter(url);
        //}
        return scrapedData;
    }

    public async Task<ScrapedData?> ExtractFromFXTwitter(Uri url)
    {
        string host = "api.fxtwitter.com";
        UriBuilder newUriBuilder = new(url)
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

            FxTwitterResponse? response = await client.GetFromJsonAsync<FxTwitterResponse>(newUrl);

            if (response != null &&
                response.Post != null)
            {
                Tweet post = response.Post;
                ScrapedData scraped = new()
                {
                    Uri = url,
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


    public async Task<ScrapedData?> ExtractFromNitter(Uri url)
    {
        foreach (string nitterInstance in _nitterInstances)
        {
            UriBuilder newUriBuilder = new(url)
            {
                Host = nitterInstance
            };

            // get a Uri instance from the UriBuilder
            Uri newUri = newUriBuilder.Uri;
            try
            {
                using HttpClient client = _httpClientFactory.CreateClient("default");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/117.0");
                HttpResponseMessage response = await client.GetAsync(newUri.AbsoluteUri);
                HtmlDocument doc = new();
                doc.Load(await response.Content.ReadAsStreamAsync());
                IEnumerable<HtmlNode> metaNodes = doc.DocumentNode.SelectSingleNode("//head").Descendants("meta");


                ScrapedData scraped = new()
                {
                    Uri = url
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
                        Media? media = await YtDownloader.DownloadVideoFromUrlAsync(url.AbsoluteUri);
                        if (media != null)
                        {
                            scraped.Medias = [media];
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
                            scraped.Medias = uriMedias
                                .Select(x => new Media { Uri = x, Type = MediaType.Image })
                                .ToList();
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