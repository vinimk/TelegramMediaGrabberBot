using CliWrap;
using CliWrap.Buffered;
using Telegram.Bot;
using TelegramMediaGrabberBot.Config;

namespace TelegramMediaGrabberBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotClient _telegramClient;


        public Worker(ILogger<Worker> logger, TelegramBotConfig telegramBotConfig, TweetinviConfig tweetinviConfig)
        {
            ArgumentNullException.ThrowIfNull(telegramBotConfig);
            ArgumentNullException.ThrowIfNull(telegramBotConfig.BotToken);

            ArgumentNullException.ThrowIfNull(tweetinviConfig);
            ArgumentNullException.ThrowIfNull(tweetinviConfig.AcessToken);
            ArgumentNullException.ThrowIfNull(tweetinviConfig.AcessTokenSecret);
            ArgumentNullException.ThrowIfNull(tweetinviConfig.ConsumerKey);
            ArgumentNullException.ThrowIfNull(tweetinviConfig.ConsumerSecret);

            _logger = logger;
            _telegramClient = new TelegramBotClient(telegramBotConfig.BotToken);
            
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {

            var url = "https://www.youtube.com/watch?v=IGQBtbKSVhY";
            var result = await Cli.Wrap("yt-dlp")
                .WithArguments(new[] {"-o", "tmp/test.%(ext)s", url })
                //.WithWorkingDirectory("/usr/local/bin")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken: cancellationToken);


            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}