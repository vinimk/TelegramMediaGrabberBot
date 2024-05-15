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
    public ScrapedDataType Type { get; set; }
    public ScrapedData()
    {
        Medias = [];
    }

    public string GetTelegramFormatedText(bool isSpoiler = false)
    {

        StringBuilder sb = new();
        if (isSpoiler)
        {
            _ = sb.Append("<span class='tg-spoiler'>");
        }

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

        if (isSpoiler)
        {
            _ = sb.Append("</span>");
        }


        return sb.Length > 4096 ?
            sb.ToString()[..4095]
            : sb.ToString();

    }

    public bool IsValid()
    {
        switch (Type)
        {
            case ScrapedDataType.Media:
                if (Medias.Count != 0)
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
        }
        return false;
    }

    public void Dispose()
    {
        Medias.ForEach(x => x.Stream?.Dispose());
        Medias.Clear();
        GC.SuppressFinalize(this);
    }
}