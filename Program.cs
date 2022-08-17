using TelegramMediaGrabberBot;
using TelegramMediaGrabberBot.Config;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        TelegramBotConfig telegramBotConfig = configuration
        .GetSection("Telegram.Bot.Config")
        .Get<TelegramBotConfig>();

        TweetinviConfig tweetinviConfig = configuration
        .GetSection("Tweetinvi.Config")
        .Get<TweetinviConfig>();

        services.AddSingleton(telegramBotConfig);
        services.AddSingleton(tweetinviConfig);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
