using System.Text;
using System.Web;
using TelegramMediaGrabberBot.DataStructures.Medias;

namespace TelegramMediaGrabberBot.DataStructures;

public class ScrapedData : IDisposable
{
    public ScrapedData()
    {
        Medias = [];
    }

    public string? Content { get; set; }
    public List<Media>? Medias { get; set; }
    public string? Author { get; set; }
    public Uri? Uri { get; set; }
    public ScrapedDataType Type { get; set; }

    public void Dispose()
    {
        Medias!.ForEach(x => x.Dispose());
        Content = null;
        Author = null;
        Uri = null;
        Medias = null;
        GC.SuppressFinalize(this);
    }

    public string GetTelegramFormatedText(bool isSpoiler = false, bool isCaption = false)
    {
        StringBuilder sb = new();
        if (isSpoiler) _ = sb.Append("<span class='tg-spoiler'>");

        if (!string.IsNullOrWhiteSpace(Author)) _ = sb.Append($"<b>{HttpUtility.HtmlEncode(Author.Trim())}</b>:\n");
        if (!string.IsNullOrWhiteSpace(Content)) _ = sb.Append($"{HttpUtility.HtmlEncode(Content.Trim())}\n");
        if (!string.IsNullOrWhiteSpace(Uri?.AbsoluteUri))
            _ = sb.Append($"<a href='{Uri?.AbsoluteUri.Trim()}'><i>Link</i></a>");

        if (isSpoiler) _ = sb.Append("</span>");

        var maxLength = isCaption ? 1024 : 4096;

        return sb.Length > maxLength
            ? sb.ToString()[..(maxLength - 1)]
            : sb.ToString();
    }

    public bool IsValid()
    {
        switch (Type)
        {
            case ScrapedDataType.Media:
                if (Medias!.Count != 0) return true;

                break;

            case ScrapedDataType.Text:
                if (!string.IsNullOrWhiteSpace(Content)) return true;

                break;
        }

        return false;
    }
}