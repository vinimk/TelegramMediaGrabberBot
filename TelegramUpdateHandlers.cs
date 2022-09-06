using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Scrapers;

namespace TelegramMediaGrabberBot
{
    public static class TelegramUpdateHandlers
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("TelegramUpdateHandlers");

        public static readonly List<long?> WhitelistedGroups;
        public static readonly List<string> SupportedWebSites;
        static TelegramUpdateHandlers()
        {
            WhitelistedGroups = new();
            SupportedWebSites = new();
        }




        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                _ => throw new NotImplementedException()
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await PollingErrorHandler(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            try
            {
                if (message.Text is not { } messageText)
                    return;

                if (WhitelistedGroups != null &&
                    !WhitelistedGroups.Contains(message.Chat.Id))
                {
                    string? notAllowedMessage = Properties.Resources.ResourceManager.GetString("GroupNotAllowed");
                    if (!string.IsNullOrEmpty(notAllowedMessage))
                    {
                        _ = await botClient.SendTextMessageAsync(message.Chat, notAllowedMessage);
                        return;
                    }
                }

                var linkParser = new Regex(@"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach (var uri in from Match match in linkParser.Matches(message.Text)
                                    let uri = new UriBuilder(match.Value).Uri
                                    select uri)
                {
                    if (!SupportedWebSites.Any(s => uri.Host.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        log.LogInformation($"Ignoring message {message.Text} because of no valid url");
                        return;
                    }

                    ScrapedData? data = null;
                    if (uri.Host.Contains("twitter.com"))
                    {
                        data = await TwitterScraper.ExtractContent(uri);
                    }
                    else if (uri.Host.Contains("instagram.com"))
                    {
                        data = await InstagramScraper.ExtractContent(uri);
                    }

                    if (data != null)
                    {
                        switch (data.Type)
                        {
                            case DataStructures.ScrapedDataType.Photo:
                                if (data.ImagesUrl != null &&
                                    data.ImagesUrl.Any())
                                {
                                    var albumMedia = new List<IAlbumInputMedia>();
                                    foreach (var imageUrl in data.ImagesUrl)
                                    {
                                        albumMedia.Add(new InputMediaPhoto(imageUrl)
                                        {
                                            Caption = data.TelegramFormatedText,
                                            ParseMode = ParseMode.Html
                                        });
                                    }

                                    if (albumMedia.Count > 0)
                                    {
                                        _ = await botClient.SendMediaGroupAsync(message.Chat, albumMedia, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                }
                                break;
                            case DataStructures.ScrapedDataType.Video:
                                if (data.VideoStream != null)
                                {
                                    var inputFile = new InputOnlineFile(data.VideoStream);
                                    _ = await botClient.SendVideoAsync(message.Chat, inputFile, caption: data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                    return;
                                }
                                break;
                            case DataStructures.ScrapedDataType.Article:
                                _ = await botClient.SendTextMessageAsync(message.Chat, data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                return;
                        }
                    }

                    #region Generic
                    var urlRequest = await GetRealUrlFromMoved(uri.AbsoluteUri);
                    using var videoStream = await YtDownloader.DownloadVideoFromUrlAsync(urlRequest);
                    if (videoStream != null)
                    {
                        log.LogInformation($"downloaded video for url {urlRequest} size: {videoStream.Length / 1024 / 1024}MB");
                        var inputFile = new InputOnlineFile(videoStream);
                        _ = await botClient.SendVideoAsync(message.Chat, inputFile, caption: urlRequest, replyToMessageId: message.MessageId);
                    }
                    else
                    {
                        log.LogError($"Stream invalid for url {uri.AbsoluteUri}");

                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unhandled exception");
            }
        }

        public static async Task<string> GetRealUrlFromMoved(string url)
        {
            //this allows you to set the settings so that we can get the redirect url
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };
            string redirectedUrl = url;

            using (HttpClient client = new(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                // ... Read the response to see if we have the redirected url
                if (response.StatusCode == System.Net.HttpStatusCode.Found ||
                    response.StatusCode == System.Net.HttpStatusCode.Moved)
                {
                    HttpResponseHeaders headers = response.Headers;
                    if (headers != null && headers.Location != null)
                    {
                        redirectedUrl = headers.Location.AbsoluteUri;
                        return await GetRealUrlFromMoved(redirectedUrl); //recursive call until we have the final url
                    }
                }
            }

            return redirectedUrl;
        }


        public static Task PollingErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}