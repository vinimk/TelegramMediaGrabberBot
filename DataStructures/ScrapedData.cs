namespace TelegramMediaGrabberBot.DataStructures
{
    public class ScrapedData : IDisposable
    {
        public string? Content { get; set; }
        public List<String>? ImagesUrl { get; set; }
        public string? Author { get; set; }
        public string? Url { get; set; }
        public ScrapedDataType? Type { get; set; }
        public Stream? VideoStream { get; set; }
        public ScrapedData()
        {
            ImagesUrl = new();
        }

        public string TelegramRawText
        {
            get
            {
                return $"{Author} :\n {Content}";
            }
        }
        public string TelegramFormatedText => $"<b>{Author}</b>:\n{Content}\n<a href='{Url}'><i>Link</i></a>";

        public void Dispose() => GC.SuppressFinalize(this);

    }
}
