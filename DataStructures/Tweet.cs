namespace TelegramMediaGrabberBot.DataStructures
{
    public class Tweet : IDisposable
    {
        public string? Content { get; set; }
        public IEnumerable<String>? ImagesUrl { get; set; }
        public string? Author { get; set; }
        public string? Url { get; set; }
        public TweetType? Type { get; set; }
        public Stream? VideoStream { get; set; }


        public string TelegramRawText
        {
            get
            {
                return $"{Author} :\n {Content}";
            }
        }
        public string TelegramFormatedText => $"<b>{Author}</b>:\n{Content}\n<a href='{Url}'><i>Link</i></a>";

        public void Dispose() => GC.SuppressFinalize(this);

        public void SetType(string? type)
        {
            ArgumentNullException.ThrowIfNull(type);
            switch (type)
            {
                case "video":
                    Type = TweetType.Video;
                    break;
                case "photo":
                    Type = TweetType.Photo;
                    break;
                case "article":
                    Type = TweetType.Article;
                    break;
                default:
                    break;
            }
        }
    }
}
