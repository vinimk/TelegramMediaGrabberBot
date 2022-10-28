using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Scrapers;

namespace TelegramMediaGrabberBot.TelegramHandler;

public class TelegramUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private IHttpClientFactory _httpClientFactory;
    private readonly Scraper _scraper;

    private static readonly Regex LinkParser = new(@"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly List<long?>? _whitelistedGroups;
    private readonly List<string> _supportedWebSites;

    public TelegramUpdateHandler(ITelegramBotClient botClient, ILogger<TelegramUpdateHandler> logger, AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(appSettings.SupportedWebSites);
        _botClient = botClient;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scraper = new Scraper(_httpClientFactory, appSettings);
        _supportedWebSites = appSettings.SupportedWebSites;
        _whitelistedGroups = appSettings.WhitelistedGroups;
    }


    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            _ => Task.CompletedTask
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        try
        {
            if (message.Text is not { } messageText)
                return;

            if (_whitelistedGroups != null &&
                _whitelistedGroups.Any() &&
                !_whitelistedGroups.Contains(message.Chat.Id))
            {
                string? notAllowedMessage = Properties.Resources.ResourceManager.GetString("GroupNotAllowed");
                if (!string.IsNullOrEmpty(notAllowedMessage))
                {
                    _ = await _botClient.SendTextMessageAsync(message.Chat, notAllowedMessage, cancellationToken: cancellationToken);
                    return;
                }
            }

            foreach (var uri in from Match match in LinkParser.Matches(message.Text)
                                let uri = new UriBuilder(match.Value).Uri
                                select uri)
            {
                if (!_supportedWebSites.Any(s => uri.AbsoluteUri.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
                {
                    _logger.LogInformation("Ignoring message {Message} because of no valid url", message.Text);
                    return;
                }

                _ = _botClient.SendChatActionAsync(message.Chat, ChatAction.Typing, cancellationToken: cancellationToken);


                ScrapedData? data = await _scraper.GetScrapedDataFromUrlAsync(uri);


                if (data != null)
                {
                    switch (data.Type)
                    {
                        case ScrapedDataType.Photo:
                            if (data.ImagesUrl != null &&
                                data.ImagesUrl.Any())
                            {
                                _ = _botClient.SendChatActionAsync(message.Chat, ChatAction.UploadPhoto, cancellationToken: cancellationToken);

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
                                    _ = await _botClient.SendMediaGroupAsync(message.Chat, albumMedia, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
                                }
                            }
                            break;
                        case ScrapedDataType.Video:
                            if (data.Video != null)
                            {
                                _ = _botClient.SendChatActionAsync(message.Chat, ChatAction.UploadVideo, cancellationToken: cancellationToken);
                                InputOnlineFile file;
                                if (data.Video.contentUri != null)
                                {
                                    file = new InputOnlineFile(data.Video.contentUri);
                                }
                                else if (data.Video.Stream != null)
                                {
                                    file = new InputOnlineFile(data.Video.Stream);
                                }
                                else
                                {
                                    _logger.LogError("url {MessageUrl} no url or stream", uri);
                                    return;
                                }
                                _ = await _botClient.SendVideoAsync(message.Chat, file, caption: data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
                            }
                            break;
                        case ScrapedDataType.Article:
                            _ = await _botClient.SendTextMessageAsync(message.Chat, data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
        }
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}