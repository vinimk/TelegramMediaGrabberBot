using System.Text;
using System.Text.Json;
using CarpaNet;
using CarpaNet.Cbor;
using CarpaNet.Json;
using CommunityToolkit.Diagnostics;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class BlueSkyScraper : ScraperBase
{
    private static ATProtoClient? _atProtocol;
    private static string? _userName;
    private static string? _passWord;

    public BlueSkyScraper(IHttpClientFactory httpClientFactory, string userName, string password) : base(
        httpClientFactory)
    {
        Guard.IsNotNullOrWhiteSpace(userName);
        Guard.IsNotNullOrWhiteSpace(password);
        _userName = userName;
        _passWord = password;
    }

    public override async Task<ScrapedData?> ExtractContentAsync(Uri postUrl, bool forceDownload = false)
    {
        _atProtocol ??= await ATProtoClient.CreateWithSessionAsync(
            identifier: _userName!,
            password: _passWord!,
            options: new ATProtoClientOptions
            {
                JsonOptions = ATProtoJsonContext.DefaultOptions,
                CborContext = ATProtoCborContext.Default,
                LoggerFactory = ApplicationLogging.LoggerFactory,
                AutoRetryOnAuthFailure = true
            });

        string user = postUrl.Segments[2];
        string rkey = postUrl.Segments[4];

        if (rkey.EndsWith('/'))
        {
            rkey = rkey[..^1];
        }

        string url = $"at://{user}app.bsky.feed.post/{rkey}";
        ATUri atUri = new(url);

        AppBsky.Feed.GetPostThreadOutput result = await _atProtocol.AppBskyFeedGetPostThreadAsync(
            new AppBsky.Feed.GetPostThreadParameters
            {
                Uri = atUri,
                Depth = 0
            });

        if (result.Thread is not AppBsky.Feed.DefsThreadViewPost thread)
        {
            return null;
        }

        AppBsky.Feed.DefsPostView post = thread.Post;
        ScrapedData scrapedData = new()
        {
            Type = ScrapedDataType.Text,
            Author = post.Author.DisplayName,
            Uri = postUrl
        };

        JsonElement postRecord = post.Record;
        string? postText = postRecord.ValueKind == JsonValueKind.Object &&
            postRecord.TryGetProperty("text", out JsonElement textElement)
                ? textElement.GetString()
                : null;

        if (postText is not null && postRecord.ValueKind == JsonValueKind.Object &&
            postRecord.TryGetProperty("facets", out JsonElement facetsElement) &&
            facetsElement.ValueKind == JsonValueKind.Array)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(postText);

            AppBsky.Richtext.Facet[] facets = facetsElement.EnumerateArray()
                .Select(AppBsky.Richtext.Facet.FromJson)
                .OrderByDescending(x => x.Index.ByteStart)
                .ToArray();

            foreach (AppBsky.Richtext.Facet facet in facets)
            {
                if (facet.Features.OfType<AppBsky.Richtext.FacetLink>().FirstOrDefault() is not { } link)
                {
                    continue;
                }

                long byteStart = facet.Index.ByteStart;
                long byteEnd = facet.Index.ByteEnd;
                if (byteStart < 0 || byteEnd < byteStart || byteEnd > textBytes.Length)
                {
                    continue;
                }

                int start = (int)byteStart;
                int end = (int)byteEnd;
                string firstPart = Encoding.UTF8.GetString(textBytes, 0, start);
                string secondPart = Encoding.UTF8.GetString(textBytes, end, textBytes.Length - end);
                textBytes = Encoding.UTF8.GetBytes($"{firstPart} {link.Uri}{secondPart}");
            }

            postText = Encoding.UTF8.GetString(textBytes);
        }

        scrapedData.Content = postText;

        if (post.Embed is AppBsky.Embed.ImagesView images)
        {
            scrapedData.Type = ScrapedDataType.Media;
            foreach (AppBsky.Embed.ImagesViewImage image in images.Images)
            {
                Media media = new()
                {
                    Type = MediaType.Image,
                    Uri = new Uri(image.Fullsize)
                };
                scrapedData.Medias!.Add(media);
            }
        }
        else if (post.Embed is AppBsky.Embed.VideoView video)
        {
            scrapedData.Type = ScrapedDataType.Media;
            MediaDetails? mediaDetails = await YtDownloader.DownloadVideoFromUrlAsync(video.Playlist, true);
            if (mediaDetails != null)
            {
                Media media = new()
                {
                    Type = MediaType.Video,
                    Stream = mediaDetails.Stream
                };
                scrapedData.Medias!.Add(media);
            }
        }
        else if (post.Embed is AppBsky.Embed.RecordWithMediaView record)
        {
            scrapedData.Type = ScrapedDataType.Media;
            if (record.Media is AppBsky.Embed.ImagesView embedImages)
            {
                foreach (AppBsky.Embed.ImagesViewImage image in embedImages.Images)
                {
                    Media media = new()
                    {
                        Type = MediaType.Image,
                        Uri = new Uri(image.Fullsize)
                    };
                    scrapedData.Medias!.Add(media);
                }
            }
            else if (record.Media is AppBsky.Embed.VideoView embedVideo)
            {
                MediaDetails? mediaDetails = await YtDownloader.DownloadVideoFromUrlAsync(embedVideo.Playlist, true);
                if (mediaDetails != null)
                {
                    Media media = new()
                    {
                        Type = MediaType.Video,
                        Stream = mediaDetails.Stream
                    };
                    scrapedData.Medias!.Add(media);
                }
            }
        }

        return scrapedData;
    }
}
