using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.TelegramHandler.Abstract;

namespace TelegramMediaGrabberBot.TelegramHandler;

// Compose Polling and ReceiverService implementations
public class TelegramPollingService(
    ILogger<TelegramPollingService> logger,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider,
    AppSettings appSettings)
    : PollingServiceBase<TelegramReceiverService>(logger, loggerFactory, serviceProvider, appSettings)
{
}