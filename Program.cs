using Telegram.Bot;
using TelegramMediaGrabberBot.Config;
using TelegramMediaGrabberBot.Services;
using TelegramMediaGrabberBot.TelegramHandler;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        ILogger logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

        IConfiguration configuration = hostContext.Configuration;

        TelegramBotConfig? telegramBotConfig = configuration
        .GetSection("Telegram.Bot.Config")
        .Get<TelegramBotConfig>();

        List<long?>? whiteListedGroups = hostContext.Configuration.GetSection("WhitelistedGroupIds").Get<List<long?>>();
        logger.LogInformation("whiteListedGroups {whiteListedGroups}", whiteListedGroups);

        List<string>? nitterInstances = hostContext.Configuration.GetSection("NitterInstances").Get<List<string>>();
        logger.LogInformation("nitterInstances {nitterInstances}", nitterInstances);

        List<string>? bibliogramInstances = hostContext.Configuration.GetSection("BibliogramInstances").Get<List<string>>();
        logger.LogInformation("bibliogramInstances {bibliogramInstances}", bibliogramInstances);

        List<string>? supportedWebSites = hostContext.Configuration.GetSection("SupportedWebSites").Get<List<string>>();
        logger.LogInformation("supportedWebSites {supportedWebSites}", supportedWebSites);

        int? hoursBetweenBackgroundTask = hostContext.Configuration.GetValue<int?>("HoursBetweenBackgroundTask");
        logger.LogInformation("HoursBetweenBackgroundTask {hoursBetweenBackgroundTask}", hoursBetweenBackgroundTask);


        AppSettings appSettings = new()
        {
            TelegramBotConfig = telegramBotConfig,
            WhitelistedGroups = whiteListedGroups,
            NitterInstances = nitterInstances,
            BibliogramInstances = bibliogramInstances,
            SupportedWebSites = supportedWebSites,
            HoursBetweenBackgroundTask = hoursBetweenBackgroundTask,
        };

        _ = services.AddSingleton<AppSettings>(appSettings);

        _ = services.AddHostedService<ClearTempBackgroundService>();

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
        //        .AddPolicyHandler(GetRetryPolicy())
        //      .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(20));

        _ = services.AddHttpClient("default",
                client =>
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36 Edg/107.0.1418.35");
                }
            );
        //.AddPolicyHandler(GetRetryPolicy())
        //        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(20)); ;


        _ = services.AddScoped<TelegramUpdateHandler>();
        _ = services.AddScoped<TelegramReceiverService>();

        _ = services.AddHostedService<TelegramPollingService>();
    })
    .Build();

await host.RunAsync();