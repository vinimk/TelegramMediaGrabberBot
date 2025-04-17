using CommunityToolkit.Diagnostics;
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
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Scraper _scraper;

    private static readonly Regex LinkParser = UrlRegex();
    private readonly List<string> _supportedWebSites;

    #endregion
    #region Construtors and handlers
    public TelegramUpdateHandler(ITelegramBotClient botClient, ILogger<TelegramUpdateHandler> logger, AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
        Guard.IsNotNull(appSettings);
        Guard.IsNotNull(appSettings.SupportedWebSites);
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scraper = new Scraper(_httpClientFactory, appSettings);
        _supportedWebSites = appSettings.SupportedWebSites;
    }

    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        _ = BotOnMessageReceived(botClient, update!.Message!, cancellationToken);
        return Task.CompletedTask;
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

    private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        try
        {
            if (message.Text is not { } messageText)
            {
                return;
            }

            if (messageText.Contains('@'))
            {
                Task action = messageText.Split('@')[0] switch
                {
                    "/acende" => SendRojao(botClient, message, cancellationToken),
                    _ => Task.CompletedTask
                };
            }

            foreach (Uri? uri in from Match match in LinkParser.Matches(messageText)
                                 let uri = new UriBuilder(match.Value).Uri
                                 select uri)
            {
                if (!_supportedWebSites.Any(s => uri.AbsoluteUri.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return;
                }
                TelegramMessageProcessor processor = new();
                await processor.ProcessMesage(_scraper, uri, message, botClient, _logger, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{message} {chatname}", message.Text, message.Chat.Title + message.Chat.Username);
        }
    }

    private static async Task SendRojao(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        _ = await botClient.SendMessage(message.Chat, "pra pra", cancellationToken: cancellationToken);
        await Task.Delay(200, cancellationToken);
        _ = await botClient.SendMessage(message.Chat, "pra", cancellationToken: cancellationToken);
        await Task.Delay(100, cancellationToken);
        _ = await botClient.SendMessage(message.Chat, "pra", cancellationToken: cancellationToken);
        await Task.Delay(100, cancellationToken);
        _ = await botClient.SendMessage(message.Chat, "pra pra pra pra pra", cancellationToken: cancellationToken);
        await Task.Delay(400, cancellationToken);
        _ = await botClient.SendMessage(message.Chat, "POOOOOWW", cancellationToken: cancellationToken);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "handle error async");
        return Task.CompletedTask;
    }
}