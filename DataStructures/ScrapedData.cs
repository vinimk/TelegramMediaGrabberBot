using System.Text;
using System.Web;
using TelegramMediaGrabberBot.DataStructures.Medias;

namespace TelegramMediaGrabberBot.DataStructures;

public class ScrapedData : IDisposable
{
    public string? Content { get; set; }
    public List<Media> Medias { get; set; }
    public string? Author { get; set; }
    public Uri? Uri { get; set; }
    public ScrapedDataType? Type { get; set; }
    public ScrapedData()
    {
        Medias = new();
    }

    public string TelegramFormatedText
    {
        get
        {
            StringBuilder sb = new();
            if (!string.IsNullOrWhiteSpace(Author))
            {
                _ = sb.Append($"<b>{HttpUtility.HtmlEncode(Author.Trim())}</b>:\n");
            }
            if (!string.IsNullOrWhiteSpace(Content))
            {
                _ = sb.Append($"{HttpUtility.HtmlEncode(Content.Trim())}\n");
            }
            if (!string.IsNullOrWhiteSpace(Uri?.AbsoluteUri))
            {
                _ = sb.Append($"<a href='{Uri?.AbsoluteUri.Trim()}'><i>Link</i></a>");
            }

            return sb.Length > 1024 ?
                sb.ToString()[..1023]
                : sb.ToString();
        }
    }

    public bool IsValid()
    {
        switch (Type)
        {
            case ScrapedDataType.Media:
                if (Medias.Any())
                {
                    return true;
                }

                break;

            case ScrapedDataType.Text:
                if (!string.IsNullOrWhiteSpace(Content))
                {
                    return true;
                }

                break;

            case null:
                break;
        }
        return false;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}