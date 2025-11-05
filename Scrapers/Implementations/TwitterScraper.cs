using System.Net.Http.Json;
using CommunityToolkit.Diagnostics;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
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

    public override async Task<ScrapedData?> ExtractContentAsync(Uri url, bool forceDownload = false)
    {
        var scrapedData = await ExtractFromFXTwitter(url);
        return scrapedData;
    }

    public async Task<ScrapedData?> ExtractFromFXTwitter(Uri url)
    {
        var host = "api.fxtwitter.com";
        UriBuilder newUriBuilder = new(url)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = host,
            Port = -1 //defualt port for schema
        };

        // get a Uri instance from the UriBuilder
        var newUrl = newUriBuilder.Uri.AbsoluteUri;

        try
        {
            using var client = _httpClientFactory.CreateClient("default");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TelegramMediaGrabberBot");

            var response = await client.GetFromJsonAsync<FxTwitterResponse>(newUrl);

            if (response != null &&
                response.Post != null)
            {
                var post = response.Post;
                ScrapedData scraped = new()
                {
                    Uri = url,
                    Content = post.Text,
                    Type = ScrapedDataType.Text
                };

                if (post.Author != null) scraped.Author = post.Author.Name;


                if (post.Media != null)
                {
                    if (post.Media.Videos != null)
                    {
                        scraped.Type = ScrapedDataType.Media;

                        foreach (var video in post.Media.Videos)
                            if (!string.IsNullOrEmpty(video.Url))
                                scraped.Medias!.Add(new Media { Type = MediaType.Video, Uri = new Uri(video.Url) });
                    }

                    if (post.Media.Photos != null)
                    {
                        scraped.Type = ScrapedDataType.Media;

                        foreach (var photo in post.Media.Photos)
                            if (!string.IsNullOrEmpty(photo.Url))
                                scraped.Medias!.Add(new Media { Type = MediaType.Image, Uri = new Uri(photo.Url) });
                    }
                }

                return scraped;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed for fxtwitter");
        } //empty catch, if there is any issue with one nitter instance, it will go to the next one

        return null;
    }
}