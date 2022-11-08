using Telegram.Bot;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.TelegramHandler;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        TelegramBotConfig? telegramBotConfig = configuration
        .GetSection("Telegram.Bot.Config")
        .Get<TelegramBotConfig>();

        List<long?>? whiteListedGroups = hostContext.Configuration.GetSection("WhitelistedGroupIds").Get<List<long?>>();

        List<string>? nitterInstances = hostContext.Configuration.GetSection("NitterInstances").Get<List<string>>();

        List<string>? bibliogramInstances = hostContext.Configuration.GetSection("BibliogramInstances").Get<List<string>>();

        List<string>? supportedWebSites = hostContext.Configuration.GetSection("SupportedWebSites").Get<List<string>>();

        AppSettings appSettings = new()
        {
            TelegramBotConfig = telegramBotConfig,
            WhitelistedGroups = whiteListedGroups,
            NitterInstances = nitterInstances,
            BibliogramInstances = bibliogramInstances,
            SupportedWebSites = supportedWebSites
        };

        _ = services.AddSingleton<AppSettings>(appSettings);


        _ = services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    if (telegramBotConfig != null && telegramBotConfig.BotToken != null)
                    {
                        TelegramBotClientOptions options = new(telegramBotConfig.BotToken);

                        return new TelegramBotClient(options, httpClient);
                    }
                    throw new NullReferenceException(nameof(telegramBotConfig));
                });

        _ = services.AddHttpClient();


        _ = services.AddScoped<TelegramUpdateHandler>();
        _ = services.AddScoped<TelegramReceiverService>();

        _ = services.AddHostedService<TelegramPollingService>();
    })
    .Build();

await host.RunAsync();
