using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot
{
    public static class TelegramUpdateHandlers
    {

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
            if (message.Text is not { } messageText)
                return;

            if (WhitelistedGroups != null &&
                !WhitelistedGroups.Contains(message.Chat.Id))
            {
                string? notAllowedMessage = Properties.Resources.ResourceManager.GetString("GroupNotAllowed");
                if (!string.IsNullOrEmpty(notAllowedMessage))
                {
                    _ = botClient.SendTextMessageAsync(message.Chat, notAllowedMessage);
                }
            }

            var linkParser = new Regex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (var uri in from Match match in linkParser.Matches(message.Text)
                                let uri = new Uri(match.Value)
                                select uri)
            {
                if (!SupportedWebSites.Any(s => uri.Host.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
                    return;

                #region twitter.com
                if (uri.Host.Contains("twitter.com"))
                {
                    var tweet = await TwitterImageScrapper.ExtractTweetContent(uri);
                    if (tweet != null)
                    {
                        switch (tweet.Type)
                        {
                            case TweetType.Photo:
                                if (tweet.ImagesUrl != null &&
                                    tweet.ImagesUrl.Any())
                                {
                                    var albumMedia = new List<IAlbumInputMedia>();
                                    foreach (var imageUrl in tweet.ImagesUrl)
                                    {
                                        albumMedia.Add(new InputMediaPhoto(imageUrl)
                                        {
                                            Caption = tweet.TelegramFormatedText,
                                            ParseMode = ParseMode.Html
                                        });
                                    }

                                    if (albumMedia.Count > 0)
                                    {
                                        _ = botClient.SendMediaGroupAsync(message.Chat, albumMedia, replyToMessageId: message.MessageId);
                                        return;
                                    }
                                }
                                break;
                            case TweetType.Video:
                                if (tweet.VideoStream != null)
                                {
                                    var inputFile = new InputOnlineFile(tweet.VideoStream);
                                    _ = botClient.SendVideoAsync(message.Chat, inputFile, caption: tweet.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                    return;
                                }
                                break;
                            case TweetType.Article:
                                _ = botClient.SendTextMessageAsync(message.Chat, tweet.TelegramFormatedText, replyToMessageId: message.MessageId);
                                return;
                        }

                    }
                }
                #endregion

                #region Generic

                using var videoStream = await YtDownloader.DownloadVideoFromUrlAsync(uri.AbsoluteUri);
                if (videoStream != null)
                {
                    var inputFile = new InputOnlineFile(videoStream);
                    _ = botClient.SendVideoAsync(message.Chat, inputFile, caption: uri.AbsoluteUri, replyToMessageId: message.MessageId);
                }
                #endregion
            }
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