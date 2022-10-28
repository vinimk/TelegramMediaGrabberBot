using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.TelegramHandler.Abstract;

namespace TelegramMediaGrabberBot.TelegramHandler;

// Compose Polling and ReceiverService implementations
public class TelegramPollingService : PollingServiceBase<TelegramReceiverService>
{
    public TelegramPollingService(ILogger<TelegramPollingService> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, AppSettings appSettings)
    : base(logger, loggerFactory, serviceProvider, appSettings)
    {
    }
}