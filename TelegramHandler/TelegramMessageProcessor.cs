using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramMediaGrabberBot.DataStructures;
using TelegramMediaGrabberBot.DataStructures.Medias;
using TelegramMediaGrabberBot.Scrapers;

namespace TelegramMediaGrabberBot.TelegramHandler;

public static class TelegramMessageProcessor
{
    public static async Task ProcessMesage(Scraper scrapper, Uri uri, Message message, ITelegramBotClient botClient, ILogger<TelegramUpdateHandler> logger, CancellationToken cancellationToken, bool forceDownload = false)
    {
        try
        {
            _ = botClient.SendChatActionAsync(chatId: message.Chat, chatAction: ChatAction.Typing, cancellationToken: cancellationToken);

            ScrapedData? data = await scrapper.GetScrapedDataFromUrlAsync(uri, forceDownload);

            if (data != null)
            {
                switch (data.Type)
                {
                    case ScrapedDataType.Media:
                        if (data.Medias != null &&
                            data.Medias.Any())
                        {
                            List<IAlbumInputMedia> albumMedia = new();
                            foreach (Media media in data.Medias)
                            {
                                ChatAction chatAction = media.Type == MediaType.Video ? ChatAction.UploadVideo : ChatAction.UploadPhoto;

                                await botClient.SendChatActionAsync(chatId: message.Chat, chatAction: chatAction, cancellationToken: cancellationToken);

                                InputFile inputFile;
                                if (media.Uri != null)
                                {
                                    inputFile = InputFile.FromUri(media.Uri);
                                }
                                else if (media.Stream != null)
                                {
                                    inputFile = InputFile.FromStream(media.Stream, Guid.NewGuid().ToString());
                                }
                                else
                                {
                                    ArgumentException argumentNullException = new("No URI or Stream for media");
                                    throw argumentNullException;
                                }

                                IAlbumInputMedia inputMedia = media.Type == MediaType.Video ? new InputMediaVideo(inputFile) : new InputMediaPhoto(inputFile);

                                //workarround for showing the caption below the album, only add it to the first message.
                                if (media == data.Medias.First())
                                {
                                    ((InputMedia)inputMedia).Caption = data.TelegramFormatedText;
                                    ((InputMedia)inputMedia).ParseMode = ParseMode.Html;
                                }
                                albumMedia.Add(inputMedia);
                            }

                            if (albumMedia.Count > 0)
                            {
                                _ = await botClient.SendMediaGroupAsync(chatId: message.Chat, media: albumMedia, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
                            }
                        }
                        break;
                    case ScrapedDataType.Text:
                        _ = await botClient.SendTextMessageAsync(chatId: message.Chat, text: data.TelegramFormatedText, parseMode: ParseMode.Html, replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
                        break;
                }
            }
            else
            {
                logger.LogError("Failed to download any data for {URL}", uri.AbsoluteUri);
            }
        }
        catch (Exception ex)
        {
            if (forceDownload == false)
            {
                logger.LogInformation("Trying to forceDownload {Message} for chatName {chatName} because of exception", message.Text, message.Chat.Title + message.Chat.Username);
                await ProcessMesage(scrapper, uri, message, botClient, logger, cancellationToken, true);
            }
            else
            {
                logger.LogError("ForceDownload also failed for {Message} for chatchanem {chatName}", message.Text, message.Chat.Title + message.Chat.Username);
                logger.LogError("ProcessMessage", ex);
            }
        }
    }
}