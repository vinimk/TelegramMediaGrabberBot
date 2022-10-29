using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot.Scrapers
{
    public class ScraperBase
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger _logger;
        public ScraperBase(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = ApplicationLogging.CreateLogger(GetType().Name);
        }

        public virtual async Task<ScrapedData?> ExtractContentAsync(Uri uri)
        {
            await Task.Delay(0);
            return new ScrapedData();
        }
    }
}
