using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers
{

    public static class InstagramScraper
    {
        private static readonly string apiSufix = "?__a=1&__d=dis";
        private static readonly ILogger log = ApplicationLogging.CreateLogger("InstagramScraper");

        public static async Task<ScrapedData?> ExtractContent(Uri instagramUrl)
        {
            var stringUrl = instagramUrl.ToString() + apiSufix;

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0");
            httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json")); //ACCEPT header
            var response = await httpClient.GetAsync(stringUrl);
            if (response.IsSuccessStatusCode)
            {
                var stringResponse = await response.Content.ReadAsStringAsync();
                dynamic result = JObject.Parse(stringResponse);
                if (result != null)
                {
                    var item = result.graphql.shortcode_media;
                    string mediaType = item.__typename;

                    ScrapedData scraped = new();

                    scraped.Author = $"{item.owner.full_name} (@{item.owner.username})";
                    scraped.Url = instagramUrl.ToString();
                    scraped.Content = item.edge_media_to_caption.edges[0].node.text;

                    switch (mediaType)
                    {
                        case "GraphImage": //photo
                            scraped.Type = ScrapedDataType.Photo;
                            if (scraped.ImagesUrl != null)
                            {
                                scraped.ImagesUrl.Add(item.display_url.ToString());
                            }
                            break;
                        case "GraphVideo": //video
                            scraped.Type = ScrapedDataType.Video;
                            var videoStream = await YtDownloader.DownloadVideoFromUrlAsync(instagramUrl.AbsoluteUri);
                            scraped.VideoStream = videoStream;
                            break;
                        case "GraphSidecar": //album
                            scraped.Type = ScrapedDataType.Photo;
                            foreach (dynamic edgeItem in item.edge_sidecar_to_children.edges)
                            {
                                if (scraped.ImagesUrl != null)
                                {
                                    scraped.ImagesUrl.Add(edgeItem.node.display_url.ToString());
                                }
                            }
                            break;
                    }
                    return scraped;
                }
            }

            return null;
        }
    }
}
