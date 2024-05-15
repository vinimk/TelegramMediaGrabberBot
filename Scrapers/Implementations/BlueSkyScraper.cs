using HtmlAgilityPack;
using System.Web;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class BlueSkyScraper(IHttpClientFactory httpClientFactory) : ScraperBase(httpClientFactory)
{
    public override async Task<ScrapedData?> ExtractContentAsync(Uri postUrl)
    {
        try
        {
            using HttpClient client = _httpClientFactory.CreateClient("default");
            HttpResponseMessage response = await client.GetAsync(postUrl.AbsoluteUri);
            HtmlDocument doc = new();
            doc.Load(await response.Content.ReadAsStreamAsync());
            IEnumerable<HtmlNode> metaNodes = doc.DocumentNode.SelectSingleNode("//head").Descendants("meta");

            ScrapedData scraped = new()
            {
                Uri = postUrl,
                Type = ScrapedDataType.Text
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


            List<Uri> uriMedias = metaNodes
             .Where(x => x.GetAttributeValue("property", null) == "og:image")
             .Select(x => x.GetAttributeValue("content", null))
             .Distinct()
             .Select(x => new Uri(x, UriKind.Absolute))
             .ToList();

            if (uriMedias.Count > 0)
            {
                scraped.Medias = uriMedias
                    .Select(x => new Media { Uri = x, Type = MediaType.Image })
                    .ToList();
                scraped.Type = ScrapedDataType.Media;
            }


            return scraped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrap bluesky {uri}", postUrl.AbsoluteUri);
        }
        return null;
    }
}