﻿using System.Text;
using System.Web;

namespace TelegramMediaGrabberBot.DataStructures;

public class ScrapedData : IDisposable
{
    public string? Content { get; set; }
    public List<string>? ImagesUrl { get; set; }
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
            if (!string.IsNullOrWhiteSpace(Author))
            {
                _ = sb.Append($"<b>{HttpUtility.HtmlEncode(Author.Trim())}</b>:\n");
            }
            if (!string.IsNullOrWhiteSpace(Content))
            {
                _ = sb.Append($"{HttpUtility.HtmlEncode(Content.Trim())}\n");
            }
            if (!string.IsNullOrWhiteSpace(Url))
            {
                _ = sb.Append($"<a href='{Url.Trim()}'><i>Link</i></a>");
            }

            return sb.Length > 1024 &&
                (Type == ScrapedDataType.Video ||
                Type == ScrapedDataType.Photo)
                ? sb.ToString()[..1023]
                : sb.Length > 2048 &&
                                Type == ScrapedDataType.Article
                    ? sb.ToString()[..2047]
                    : sb.ToString();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

