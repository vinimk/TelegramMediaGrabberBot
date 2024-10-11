using CommunityToolkit.Diagnostics;
using FishyFlip;
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
            _ = await _atProtocol.AuthenticateWithPasswordAsync(userName, password);
        }

        string user = postUrl.Segments[2];
        string rkey = postUrl.Segments[4];

        if (rkey.EndsWith('/'))
        {
            rkey = rkey.Remove(rkey.Length - 1, 1);
        }

        string url = $"at://{user}app.bsky.feed.post/{rkey}";
        ATUri atUri = new(url);

        Result<ThreadPostViewFeed> result = await _atProtocol.Feed.GetPostThreadAsync(atUri, 0);

        PostView post = ((ThreadPostViewFeed)result.Value!).Thread.Post!;

        ScrapedData scrapedData = new()
        {
            Type = ScrapedDataType.Text,
            Content = post.Record?.Text,
            Author = post.Author.DisplayName,
            Uri = postUrl
        };

        if (post.Embed is ImageViewEmbed images)
        {
            scrapedData.Type = ScrapedDataType.Media;
            foreach (ImageView image in images.Images)
            {
                Media media = new()
                {
                    Type = MediaType.Image,
                    Uri = new Uri(image.Fullsize)
                };
                scrapedData.Medias.Add(media);
            }
        }
        else if (post.Embed is VideoViewEmbed video)
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
        else if (post.Embed is RecordWithMediaViewEmbed record)
        {
            scrapedData.Type = ScrapedDataType.Media;
            if (record.Embed is ImageViewEmbed embedImages)
            {
                foreach (ImageView image in embedImages.Images)
                {
                    Media media = new()
                    {
                        Type = MediaType.Image,
                        Uri = new Uri(image.Fullsize)
                    };
                    scrapedData.Medias.Add(media);
                }
            }
            else if (record.Embed is VideoViewEmbed embedVideo)
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