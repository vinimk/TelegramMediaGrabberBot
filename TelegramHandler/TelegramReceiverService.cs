using Telegram.Bot;
using TelegramMediaGrabberBot.TelegramHandler.Abstract;

namespace TelegramMediaGrabberBot.TelegramHandler;

// Compose Receiver and UpdateHandler implementation
public class TelegramReceiverService(
    ITelegramBotClient botClient,
    TelegramUpdateHandler updateHandler,
    ILogger<ReceiverServiceBase<TelegramUpdateHandler>> logger)
    : ReceiverServiceBase<TelegramUpdateHandler>(botClient, updateHandler, logger)
{
}