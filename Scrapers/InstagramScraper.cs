﻿using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers
{

    public static class InstagramScraper
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("InstagramScraper");
        public static readonly List<string?> BibliogramInstances;
        static InstagramScraper() => BibliogramInstances = new();

        public static async Task<ScrapedData?> ExtractContent(Uri instagramUrl)
        {
            if (BibliogramInstances != null)
            {
                foreach (var bibliogramInstance in BibliogramInstances)
                {
                    try
                    {
                        var newUriBuilder = new UriBuilder(instagramUrl)
                        {
                            Host = bibliogramInstance
                        };

                        // get a Uri instance from the UriBuilder
                        var newUri = newUriBuilder.Uri;


                        using HttpClient client = new();
                        var response = await client.GetAsync(newUri.AbsoluteUri);
                        if (response.IsSuccessStatusCode)
                        {
                            var doc = new HtmlDocument();
                            doc.Load(await response.Content.ReadAsStreamAsync());

                            ScrapedData scraped = new()
                            {
                                Url = instagramUrl.AbsoluteUri
                            };

                            string content = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//p[@class='structured-text description']").InnerText);

                            scraped.Content = content;

                            string tweetAuthor = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//a[@class='name']").InnerText);

                            scraped.Author = tweetAuthor;

                            string tweetType = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", null));

                            if (tweetType.StartsWith("Video by"))
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
                                    log.LogError($"Failed to download video from mirror {bibliogramInstance}, trying original instagram");
                                    videoStream = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
                                    if (videoStream == null) //if it also fails from instagram, try the other bibliogram instance
                                    {
                                        continue;
                                    }
                                }
                                scraped.VideoStream = videoStream;
                            }
                            else if (tweetType.StartsWith("Photo by") ||
                                tweetType.StartsWith("Post by"))
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
            }
            return null;
        }
    }
}
