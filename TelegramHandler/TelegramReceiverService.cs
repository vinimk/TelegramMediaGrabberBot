using Telegram.Bot;
using TelegramMediaGrabberBot.TelegramHandler.Abstract;

namespace TelegramMediaGrabberBot.TelegramHandler;

// Compose Receiver and UpdateHandler implementation
public class TelegramReceiverService : ReceiverServiceBase<TelegramUpdateHandler>
{
    public TelegramReceiverService(
        ITelegramBotClient botClient,
        TelegramUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<TelegramUpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}