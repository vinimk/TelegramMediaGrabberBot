using TelegramMediaGrabberBot;
using TelegramMediaGrabberBot.Config;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        TelegramBotConfig telegramBotConfig = configuration
        .GetSection("Telegram.Bot.Config")
        .Get<TelegramBotConfig>();

        var whiteListedGroups = hostContext.Configuration.GetSection("WhitelistedGroupIds").Get<List<long?>>();

        var nitterInstances = hostContext.Configuration.GetSection("NitterInstances").Get<List<string?>>();

        var supportedWebSites = hostContext.Configuration.GetSection("SupportedWebSites").Get<List<string>>();

        AppSettings appSettings = new()
        {
            TelegramBotConfig = telegramBotConfig,
            WhitelistedGroups = whiteListedGroups,
            NitterInstances = nitterInstances,
            SupportedWebSites = supportedWebSites
        }; ;
        services.AddSingleton<AppSettings>(appSettings);

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
