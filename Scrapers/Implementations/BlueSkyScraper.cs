using CommunityToolkit.Diagnostics;
using FishyFlip;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
using FishyFlip.Lexicon.App.Bsky.Richtext;
using FishyFlip.Models;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.Scrapers.Implementations;

public class BlueSkyScraper : ScraperBase
{
    private static ATProtocol? _atProtocol;
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
        if (_atProtocol == null)
        {
            var atProtocolBuilder = new ATProtocolBuilder()
                .EnableAutoRenewSession(true)
                // Set the instance URL for the PDS you wish to connect to.
                // Defaults to bsky.social.
                .WithLogger(_logger);
            _atProtocol = atProtocolBuilder.Build();
            _ = await _atProtocol.AuthenticateWithPasswordResultAsync(_userName!, _passWord!);
        }

        var user = postUrl.Segments[2];
        var rkey = postUrl.Segments[4];

        if (rkey.EndsWith('/')) rkey = rkey[..^1];

        var url = $"at://{user}app.bsky.feed.post/{rkey}";
        ATUri atUri = new(url);

        var result = await _atProtocol.Feed.GetPostThreadAsync(atUri, 0);

        if (result.Value is ExpiredTokenError) //workarround for autorenewal not working
        {
            _logger.LogInformation("Token expired, setting protocol to null {token}", result.Value.ToString());
            _atProtocol = null;
            return await ExtractContentAsync(postUrl);
        }

        var post = ((ThreadViewPost)((GetPostThreadOutput)result.Value!).Thread!).Post!;

        ScrapedData scrapedData = new()
        {
            Type = ScrapedDataType.Text,
            Author = post.Author!.DisplayName,
            Uri = postUrl
        };

        var postText = post.PostRecord!.Text;

        if (post.PostRecord.Facets != null)
        {
            var urlsInText = post.PostRecord!.Facets.Where(x => x.Type == "app.bsky.richtext.facet");

            foreach (var facet in urlsInText)
            {
                var start = (int)facet.Index.ByteStart;
                var end = (int)facet.Index.ByteEnd;
                var length = end - start;
                if (facet.Features!.FirstOrDefault(x => x is Link) is Link link && postText != null)
                {
                    var replecaement = link.Uri;
                    // Extract the part before the replacement
                    var firstPart = postText[..start];

                    // Extract the part after the replacement (if any)
                    var secondPart = postText[(start + length)..];

                    // Concatenate to form the new string
                    var newString = firstPart + replecaement + secondPart;

                    postText = newString;
                }
            }
        }

        scrapedData.Content = postText;

        if (post.Embed is ViewImages images)
        {
            scrapedData.Type = ScrapedDataType.Media;
            foreach (var image in images.Images!)
            {
                Media media = new()
                {
                    Type = MediaType.Image,
                    Uri = new Uri(image.Fullsize!)
                };
                scrapedData.Medias!.Add(media);
            }
        }
        else if (post.Embed is ViewVideo video)
        {
            scrapedData.Type = ScrapedDataType.Media;
            var mediaDetails = await YtDownloader.DownloadVideoFromUrlAsync(video.Playlist!, true);
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
        else if (post.Embed is ViewRecordWithMedia record)
        {
            scrapedData.Type = ScrapedDataType.Media;
            if (record.Media is ViewImages embedImages)
            {
                foreach (var image in embedImages.Images!)
                {
                    Media media = new()
                    {
                        Type = MediaType.Image,
                        Uri = new Uri(image.Fullsize!)
                    };
                    scrapedData.Medias!.Add(media);
                }
            }
            else if (record.Media is ViewVideo embedVideo)
            {
                var mediaDetails = await YtDownloader.DownloadVideoFromUrlAsync(embedVideo.Playlist!, true);
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