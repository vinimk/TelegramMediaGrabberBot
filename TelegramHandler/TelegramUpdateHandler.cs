using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.Scrapers;

namespace TelegramMediaGrabberBot.TelegramHandler;

public partial class TelegramUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Scraper _scraper;

    private static readonly Regex LinkParser = MyRegex();
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
        Task handler = update switch
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
            {
                return;
            }

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

            foreach (Uri? uri in from Match match in LinkParser.Matches(message.Text)
                                 let uri = new UriBuilder(match.Value).Uri
                                 select uri)
            {
                if (!_supportedWebSites.Any(s => uri.AbsoluteUri.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
                {
                    _logger.LogInformation("Ignoring message {Message} for chatName {chatName} because of no valid url", message.Text, message.Chat.Title + message.Chat.Username);
                    return;
                }
                else
                {
                    _logger.LogInformation("Processing {URL} for chatName {chatName}", uri.AbsoluteUri, message.Chat.Title + message.Chat.Username);
                }

                _ = _botClient.SendChatActionAsync(message.Chat, ChatAction.Typing, cancellationToken: cancellationToken);


                ScrapedData? data = await _scraper.GetScrapedDataFromUrlAsync(uri);


                if (data != null)
                {
                    switch (data.Type)
                    {
                        case ScrapedDataType.Photo:
                            if (data.Medias != null &&
                                data.Medias.Any())
                            {
                                _ = _botClient.SendChatActionAsync(message.Chat, ChatAction.UploadPhoto, cancellationToken: cancellationToken);

                                List<IAlbumInputMedia> albumMedia = new();
                                foreach (ScrapedData.Media media in data.Medias)
                                {
                                    if (media.Uri != null)
                                    {
                                        InputFileUrl inputFileUrl = new(media.Uri);
                                        IAlbumInputMedia inputMedia = media.Type == ScrapedDataType.Video ? new InputMediaVideo(inputFileUrl) : new InputMediaPhoto(inputFileUrl);

                                        //workarround for showing the caption below the album, only add it to the first message.
                                        if (media == data.Medias.First())
                                        {
                                            ((InputMedia)inputMedia).Caption = data.TelegramFormatedText;
                                            ((InputMedia)inputMedia).ParseMode = ParseMode.Html;
                                        }
                                        albumMedia.Add(inputMedia);
                                    }
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
                                IInputFile file;
                                if (data.Video.contentUri != null)
                                {
                                    file = new InputFileUrl(data.Video.contentUri.AbsoluteUri);
                                }
                                else if (data.Video.Stream != null)
                                {
                                    file = new InputFile(data.Video.Stream);
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
                else
                {
                    _logger.LogError("Failed to download any data for {URL}", uri.AbsoluteUri);
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
        string ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    [GeneratedRegex("[(http(s)?):\\/\\/(www\\.)?a-zA-Z0-9@:%._\\+~#=]{2,256}\\.[a-z]{2,6}\\b([-a-zA-Z0-9@:%_\\+.~#?&//=]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-GB")]
    private static partial Regex MyRegex();
}