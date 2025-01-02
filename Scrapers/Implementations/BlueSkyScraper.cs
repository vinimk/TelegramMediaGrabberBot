using CommunityToolkit.Diagnostics;
using FishyFlip;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Models;
using Microsoft.Extensions.Logging.Debug;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class BlueSkyScraper(IHttpClientFactory httpClientFactory, string userName, string password) : ScraperBase(httpClientFactory)
{
    private static ATProtocol? _atProtocol;
    public override async Task<ScrapedData?> ExtractContentAsync(Uri postUrl)
    {
        if (_atProtocol == null)
        {
            Guard.IsNotNullOrWhiteSpace(userName);
            Guard.IsNotNullOrWhiteSpace(password);
            // Include a ILogger if you want additional logging from the base library.
            DebugLoggerProvider debugLog = new();
            ATProtocolBuilder atProtocolBuilder = new ATProtocolBuilder()
                .EnableAutoRenewSession(true)
                // Set the instance URL for the PDS you wish to connect to.
                // Defaults to bsky.social.
                .WithLogger(debugLog.CreateLogger("FishyFlipDebug"));
            _atProtocol = atProtocolBuilder.Build();
            _ = await _atProtocol.AuthenticateWithPasswordResultAsync(userName, password);
        }

        string user = postUrl.Segments[2];
        string rkey = postUrl.Segments[4];

        if (rkey.EndsWith('/'))
        {
            rkey = rkey.Remove(rkey.Length - 1, 1);
        }

        string url = $"at://{user}app.bsky.feed.post/{rkey}";
        ATUri atUri = new(url);

        Result<GetPostThreadOutput?> result = await _atProtocol.Feed.GetPostThreadAsync(atUri, 0);

        if (result.IsT1)
        {
            _logger.LogError("status code: {status}, details: {details}", result.AsT1.StatusCode, result.AsT1.Detail);
            return null;
        }

        PostView post = ((ThreadViewPost)((GetPostThreadOutput)result.Value!).Thread!).Post!;

        ScrapedData scrapedData = new()
        {
            Type = ScrapedDataType.Text,
            Content = post.PostRecord!.Text,
            Author = post.Author!.DisplayName,
            Uri = postUrl
        };

        if (post.Embed is ViewImages images)
        {
            scrapedData.Type = ScrapedDataType.Media;
            foreach (ViewImage image in images.Images!)
            {
                Media media = new()
                {
                    Type = MediaType.Image,
                    Uri = new Uri(image.Fullsize!)
                };
                scrapedData.Medias.Add(media);
            }
        }
        else if (post.Embed is ViewVideo video)
        {
            scrapedData.Type = ScrapedDataType.Media;
            MediaDetails? mediaDetails = await YtDownloader.DownloadVideoFromUrlAsync(video.Playlist!, true);
            if (mediaDetails != null)
            {
                Media media = new()
                {
                    Type = MediaType.Video,
                    Stream = mediaDetails.Stream
                };
                scrapedData.Medias.Add(media);
            }
        }
        else if (post.Embed is ViewRecordWithMedia record)
        {
            scrapedData.Type = ScrapedDataType.Media;
            if (record.Media is ViewImages embedImages)
            {
                foreach (ViewImage image in embedImages.Images!)
                {
                    Media media = new()
                    {
                        Type = MediaType.Image,
                        Uri = new Uri(image.Fullsize!)
                    };
                    scrapedData.Medias.Add(media);
                }
            }
            else if (record.Media is ViewVideo embedVideo)
            {
                MediaDetails? mediaDetails = await YtDownloader.DownloadVideoFromUrlAsync(embedVideo.Playlist!, true);
                if (mediaDetails != null)
                {
                    Media media = new()
                    {
                        Type = MediaType.Video,
                        Stream = mediaDetails.Stream
                    };
                    scrapedData.Medias.Add(media);
                }
            }
        }
        return scrapedData;
    }
}