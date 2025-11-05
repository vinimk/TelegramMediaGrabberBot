using Telegram.Bot;
using Telegram.Bot.Polling;

namespace TelegramMediaGrabberBot.TelegramHandler.Abstract;

/// <summary>
///     An abstract class to compose Receiver Service and Update Handler classes
/// </summary>
/// <typeparam name="TUpdateHandler">Update Handler to use in Update Receiver</typeparam>
public abstract class ReceiverServiceBase<TUpdateHandler>(
    ITelegramBotClient botClient,
    TUpdateHandler updateHandler,
    ILogger<ReceiverServiceBase<TUpdateHandler>> logger) : IReceiverService
    where TUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient = botClient;
    private readonly ILogger<ReceiverServiceBase<TUpdateHandler>> _logger = logger;
    private readonly IUpdateHandler _updateHandlers = updateHandler;

    /// <summary>
    ///     Start to service Updates with provided Update Handler class
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        try
        {
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = [],
                DropPendingUpdates = true
            };

            var me = await _botClient.GetMe(stoppingToken);
            _logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? "My Awesome Bot");


            // Start receiving updates
            await _botClient.ReceiveAsync(
                _updateHandlers,
                receiverOptions,
                stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error receiving messages {ex}", ex);
        }
    }
}