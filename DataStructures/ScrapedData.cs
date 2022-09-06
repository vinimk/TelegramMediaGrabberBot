using System.Text;

namespace TelegramMediaGrabberBot.DataStructures
{
    public class ScrapedData : IDisposable
    {
        public string? Content { get; set; }
        public List<String>? ImagesUrl { get; set; }
        public string? Author { get; set; }
        public string? Url { get; set; }
        public ScrapedDataType? Type { get; set; }
        public Video? Video { get; set; }
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
        public string TelegramFormatedText
        {
            get
            {
                StringBuilder sb = new();
                if (!String.IsNullOrWhiteSpace(Author))
                {
                    sb.Append($"<b>{Author.Trim()}</b>:\n");
                }
                if (!String.IsNullOrWhiteSpace(Content))
                {
                    sb.Append($"{Content.Trim()}\n");
                }
                if (!String.IsNullOrWhiteSpace(Url))
                {
                    sb.Append($"<a href='{Url.Trim()}'><i>Link</i></a>");
                }
                return sb.ToString();
            }
        }

        public void Dispose() => GC.SuppressFinalize(this);

    }
}
