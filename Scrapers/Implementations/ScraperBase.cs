using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers.Implementations
{
    public abstract class ScraperBase
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        public ScraperBase(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = ApplicationLogging.CreateLogger(GetType().Name);
        }

        public virtual async Task<ScrapedData?> ExtractContentAsync(Uri uri, bool forceDownload = false)
        {
            await Task.Delay(0);
            return new ScrapedData();
        }
    }
}