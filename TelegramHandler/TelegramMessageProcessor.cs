using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Scrapers;
using TelegramMediaGrabberBot.Utils;

namespace TelegramMediaGrabberBot.TelegramHandler;

public static class TelegramMessageProcessor
{
    public static async Task ProcessMesage(Scraper scrapper, Uri uri, Message message, ITelegramBotClient botClient,
        ILogger<TelegramUpdateHandler> logger, CancellationToken cancellationToken, bool forceDownload = false)
    {
        List<IAlbumInputMedia> albumMedia = [];
        try
        {
            var messageThreadId = message.IsTopicMessage &&
                                  message.MessageThreadId is not null and > 0
                ? message.MessageThreadId
                : null;

            await botClient.SendChatAction(message.Chat, messageThreadId: messageThreadId, action: ChatAction.Typing,
                cancellationToken: cancellationToken);

            using var data = await scrapper.GetScrapedDataFromUrlAsync(uri, forceDownload);

            if (data != null)
            {
                var isSpoiler = message.Entities != null &&
                                message.Entities.Any(x => x.Type == MessageEntityType.Spoiler);

                switch (data.Type)
                {
                    case ScrapedDataType.Media:
                        try
                        {
                            if (data.Medias!.Count != 0)
                            {
                                foreach (var media in data.Medias)
                                {
                                    var chatAction = media.Type == MediaType.Video
                                        ? ChatAction.UploadVideo
                                        : ChatAction.UploadPhoto;

                                    await botClient.SendChatAction(message.Chat, messageThreadId: messageThreadId,
                                        action: chatAction, cancellationToken: cancellationToken);

                                    InputFile inputFile;
                                    if (media.Uri != null)
                                    {
                                        inputFile = InputFile.FromUri(media.Uri);
                                    }
                                    else if (media.Stream != null &&
                                             media.Stream.Length <= 52428800) //50mb, limit of telegram bot
                                    {
                                        inputFile = InputFile.FromStream(media.Stream, Guid.NewGuid().ToString());
                                    }
                                    else
                                    {
                                        ArgumentException argumentNullException = new("No URI or Stream for media");
                                        throw argumentNullException;
                                    }

                                    IAlbumInputMedia inputMedia = media.Type == MediaType.Video
                                        ? new InputMediaVideo(inputFile) { HasSpoiler = isSpoiler }
                                        : new InputMediaPhoto(inputFile) { HasSpoiler = isSpoiler };

                                    //workarround for showing the caption below the album, only add it to the first message.
                                    if (media == data.Medias.First())
                                    {
                                        ((InputMedia)inputMedia).Caption =
                                            data.GetTelegramFormatedText(isSpoiler, true);
                                        ((InputMedia)inputMedia).ParseMode = ParseMode.Html;
                                    }

                                    albumMedia.Add(inputMedia);
                                }

                                try
                                {
                                    _ = await botClient.SendMediaGroup(message.Chat, messageThreadId: messageThreadId,
                                        media: albumMedia, cancellationToken: cancellationToken);
                                }
                                catch (Exception)
                                {
                                    //workarround for telegram failing to download media directly
                                    logger.LogInformation(
                                        "Telegram download fail, trying direct download of media before sending to telegram url:{url}",
                                        uri);

                                    albumMedia = [];

                                    foreach (var media in
                                             data.Medias.Where(x =>
                                                 x.Type == MediaType.Video)) //only try to force for videos for now
                                    {
                                        var stream = await HttpUtils.GetStreamFromUrl(media.Uri!);
                                        var inputFile = InputFile.FromStream(stream!, Guid.NewGuid().ToString());
                                        InputMediaVideo inputMedia = new(inputFile)
                                        {
                                            HasSpoiler = isSpoiler,
                                            Caption = data.GetTelegramFormatedText(isSpoiler, true),
                                            ParseMode = ParseMode.Html
                                        };

                                        albumMedia.Add(inputMedia);
                                    }

                                    _ = await botClient.SendMediaGroup(message.Chat, messageThreadId: messageThreadId,
                                        media: albumMedia, cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogInformation(ex, "{uri} error, trying forceDownload", uri.AbsoluteUri);

                            if (!forceDownload)
                                await ProcessMesage(scrapper, uri, message, botClient, logger, cancellationToken, true);
                        }

                        break;
                    case ScrapedDataType.Text:
                        _ = await botClient.SendMessage(message.Chat, messageThreadId: messageThreadId,
                            text: data.GetTelegramFormatedText(isSpoiler), parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                        break;
                }
            }
            else
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed for {Message} for chat {chatName}, {threadId}", message.Text,
                message.Chat.Title + message.Chat.Username, message.MessageThreadId);
        }
        finally
        {
            albumMedia
                .Select(x => x as InputMedia)
                .Where(x => x != null && x.Media.FileType == FileType.Stream)
                .Select(x => x!.Media as InputFileStream)
                .ToList()
                .ForEach(x => x!.Content.Dispose());
        }
    }
}