using Telegram.Bot;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.TelegramHandler;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        TelegramBotConfig telegramBotConfig = configuration
        .GetSection("Telegram.Bot.Config")
        .Get<TelegramBotConfig>();

        var whiteListedGroups = hostContext.Configuration.GetSection("WhitelistedGroupIds").Get<List<long?>>();

        var nitterInstances = hostContext.Configuration.GetSection("NitterInstances").Get<List<string?>>();

        var bibliogramInstances = hostContext.Configuration.GetSection("BibliogramInstances").Get<List<string?>>();

        var supportedWebSites = hostContext.Configuration.GetSection("SupportedWebSites").Get<List<string>>();

        AppSettings appSettings = new()
        {
            TelegramBotConfig = telegramBotConfig,
            WhitelistedGroups = whiteListedGroups,
            NitterInstances = nitterInstances,
            BibliogramInstances = bibliogramInstances,
            SupportedWebSites = supportedWebSites
        };

        services.AddSingleton<AppSettings>(appSettings);


        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    if (telegramBotConfig != null && telegramBotConfig.BotToken != null)
                    {
                        TelegramBotClientOptions options = new(telegramBotConfig.BotToken);

                        return new TelegramBotClient(options, httpClient);
                    }
                    throw new NullReferenceException(nameof(telegramBotConfig));
                });

        services.AddHttpClient();


        services.AddScoped<TelegramUpdateHandler>();
        services.AddScoped<TelegramReceiverService>();

        services.AddHostedService<TelegramPollingService>();
    })
    .Build();

await host.RunAsync();
