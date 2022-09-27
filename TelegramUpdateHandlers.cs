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
        private static readonly Regex LinkParser = new(@"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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

                foreach (var uri in from Match match in LinkParser.Matches(message.Text)
                                    let uri = new UriBuilder(match.Value).Uri
                                    select uri)
                {
                    if (!SupportedWebSites.Any(s => uri.AbsoluteUri.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        log.LogInformation($"Ignoring message {message.Text} because of no valid url");
                        return;
                    }

                    ScrapedData? data = null;
                    if (uri.AbsoluteUri.Contains("twitter.com"))
                        data = await TwitterScraper.ExtractContent(uri);
                    else if (uri.AbsoluteUri.Contains("instagram.com"))
                        data = await InstagramScraper.ExtractContent(uri);
                    else
                        data = await GenericScrapper.ExtractContent(uri);

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
                                if (data.Video != null &&
                                    data.Video.Stream != null)
                                {
                                    var inputFile = new InputOnlineFile(data.Video.Stream);
                                    _ = await botClient.SendVideoAsync(message.Chat, inputFile, caption: data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                    return;
                                }
                                break;
                            case DataStructures.ScrapedDataType.Article:
                                _ = await botClient.SendTextMessageAsync(message.Chat, data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId);
                                return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unhandled exception");
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