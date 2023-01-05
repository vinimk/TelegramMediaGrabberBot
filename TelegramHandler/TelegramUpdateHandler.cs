using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.Scrapers;

namespace TelegramMediaGrabberBot.TelegramHandler;

public partial class TelegramUpdateHandler : IUpdateHandler
{
    #region variables
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Scraper _scraper;

    private static readonly Regex LinkParser = UrlRegex();
    private readonly List<long?>? _whitelistedGroups;
    private readonly List<string> _supportedWebSites;

    #endregion
    #region Construtors and handlers
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
    private static partial Regex UrlRegex();

    #endregion

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

                await TelegramMessageProcessor.ProcessMesage(_scraper, uri, message, _botClient, _logger, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
        }
    }
}