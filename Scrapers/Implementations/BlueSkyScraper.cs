﻿using CommunityToolkit.Diagnostics;
using FishyFlip;
using FishyFlip.Lexicon;
using FishyFlip.Lexicon.App.Bsky.Embed;
using FishyFlip.Lexicon.App.Bsky.Feed;
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

    public BlueSkyScraper(IHttpClientFactory httpClientFactory, string userName, string password) : base(httpClientFactory)
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
            ATProtocolBuilder atProtocolBuilder = new ATProtocolBuilder()
                .EnableAutoRenewSession(true)
                // Set the instance URL for the PDS you wish to connect to.
                // Defaults to bsky.social.
                .WithLogger(_logger);
            _atProtocol = atProtocolBuilder.Build();
            _ = await _atProtocol.AuthenticateWithPasswordResultAsync(_userName!, _passWord!);
        }

        string user = postUrl.Segments[2];
        string rkey = postUrl.Segments[4];

        if (rkey.EndsWith('/'))
        {
            rkey = rkey[..^1];
        }

        string url = $"at://{user}app.bsky.feed.post/{rkey}";
        ATUri atUri = new(url);

        Result<GetPostThreadOutput?> result = await _atProtocol.Feed.GetPostThreadAsync(atUri, 0);

        if (result.Value is ExpiredTokenError) //workarround for autorenewal not working
        {
            _logger.LogInformation("Token expired, setting protocol to null {token}", result.Value.ToString());
            _atProtocol = null;
            return await ExtractContentAsync(postUrl);
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
                scrapedData.Medias!.Add(media);
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
                scrapedData.Medias!.Add(media);
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
                    scrapedData.Medias!.Add(media);
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
                    scrapedData.Medias!.Add(media);
                }
            }
        }
        return scrapedData;
    }
}
