using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramMediaGrabberBot.Config;

namespace TelegramMediaGrabberBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotClient _telegramClient;


        public Worker(ILogger<Worker> logger, AppSettings appSettings)
        {
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(appSettings.TelegramBotConfig);
            ArgumentNullException.ThrowIfNull(appSettings.TelegramBotConfig.BotToken);
            ArgumentNullException.ThrowIfNull(appSettings.WhitelistedGroups);
            ArgumentNullException.ThrowIfNull(appSettings.NitterInstances);
            ArgumentNullException.ThrowIfNull(appSettings.SupportedWebSites);

            _logger = logger;
            _telegramClient = new TelegramBotClient(appSettings.TelegramBotConfig.BotToken);

            TelegramUpdateHandlers.WhitelistedGroups.AddRange(appSettings.WhitelistedGroups);

            TwitterImageScrapper.NitterInstances.AddRange(appSettings.NitterInstances);

            TelegramUpdateHandlers.SupportedWebSites.AddRange(appSettings.SupportedWebSites);


            using var cts = new CancellationTokenSource();
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>(),
                ThrowPendingUpdates = true,
            };

            _telegramClient.StartReceiving(updateHandler: TelegramUpdateHandlers.HandleUpdateAsync,
                               pollingErrorHandler: TelegramUpdateHandlers.PollingErrorHandler,
                               receiverOptions: receiverOptions,
                               cancellationToken: cts.Token);

        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(3000, cancellationToken);
            }
        }
    }
}