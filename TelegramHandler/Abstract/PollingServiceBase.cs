using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.Scrapers;

namespace TelegramMediaGrabberBot.TelegramHandler.Abstract;

// A background service consuming a scoped service.
// See more: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services#consuming-a-scoped-service-in-a-background-task
/// <summary>
/// An abstract class to compose Polling background service and Receiver implementation classes
/// </summary>
/// <typeparam name="TReceiverService">Receiver implementation class</typeparam>
public abstract class PollingServiceBase<TReceiverService> : BackgroundService
    where TReceiverService : IReceiverService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public PollingServiceBase(ILogger<PollingServiceBase<TReceiverService>> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(appSettings.WhitelistedGroups);
        ArgumentNullException.ThrowIfNull(appSettings.NitterInstances);
        ArgumentNullException.ThrowIfNull(appSettings.BibliogramInstances);
        ArgumentNullException.ThrowIfNull(appSettings.SupportedWebSites);
        _serviceProvider = serviceProvider;
        _logger = logger;

        ApplicationLogging.LoggerFactory = loggerFactory;

        TwitterScraper.NitterInstances.AddRange(appSettings.NitterInstances);

        InstagramScraper.BibliogramInstances.AddRange(appSettings.BibliogramInstances);

        YtDownloader.UpdateYtDlpAsync().Wait();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");

        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        // Make sure we receive updates until Cancellation Requested,
        // no matter what errors our ReceiveAsync get
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create new IServiceScope on each iteration.
                // This way we can leverage benefits of Scoped TReceiverService
                // and typed HttpClient - we'll grab "fresh" instance each time
                using var scope = _serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<TReceiverService>();

                await receiver.ReceiveAsync(stoppingToken);
            }
            // Update Handler only captures exception inside update polling loop
            // We'll catch all other exceptions here
            // see: https://github.com/TelegramBots/Telegram.Bot/issues/1106
            catch (Exception ex)
            {
                _logger.LogError("Polling failed with exception: {Exception}", ex);

                // Cooldown if something goes wrong
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
