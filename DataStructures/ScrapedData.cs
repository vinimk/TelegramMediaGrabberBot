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

                if (sb.Length > 1024 &&
                    (this.Type == ScrapedDataType.Video ||
                    this.Type == ScrapedDataType.Photo)
                    )
                {
                    return sb.ToString().Substring(0, 1023);
                }
                else if (sb.Length > 2048 &&
                    this.Type == ScrapedDataType.Article)
                {
                    return sb.ToString().Substring(0, 2047);
                }
                else
                {
                    return sb.ToString();
                }
            }
        }

        public void Dispose() => GC.SuppressFinalize(this);

    }
}
