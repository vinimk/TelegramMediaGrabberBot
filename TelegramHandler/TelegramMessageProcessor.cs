﻿using Telegram.Bot;
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
            _ = botClient.SendChatActionAsync(chatId: message.Chat, messageThreadId: message.MessageThreadId, chatAction: ChatAction.Typing, cancellationToken: cancellationToken);

            ScrapedData? data = await scrapper.GetScrapedDataFromUrlAsync(uri, forceDownload);

            if (data != null)
            {
                int? messageThreadId = null;
                if (message.MessageThreadId is not null and
                    > 0)
                {
                    messageThreadId = message.MessageThreadId;
                }

                bool isSpoiler = message.Entities != null && message.Entities.Any(x => x.Type == MessageEntityType.Spoiler);

                switch (data.Type)
                {
                    case ScrapedDataType.Media:
                        if (data.Medias.Any())
                        {
                            List<IAlbumInputMedia> albumMedia = new();
                            foreach (Media media in data.Medias)
                            {
                                ChatAction chatAction = media.Type == MediaType.Video ? ChatAction.UploadVideo : ChatAction.UploadPhoto;

                                await botClient.SendChatActionAsync(chatId: message.Chat, messageThreadId: messageThreadId, chatAction: chatAction, cancellationToken: cancellationToken);

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

                                IAlbumInputMedia inputMedia = media.Type == MediaType.Video ? new InputMediaVideo(inputFile) { HasSpoiler = isSpoiler } : new InputMediaPhoto(inputFile) { HasSpoiler = isSpoiler };

                                //workarround for showing the caption below the album, only add it to the first message.
                                if (media == data.Medias.First())
                                {
                                    ((InputMedia)inputMedia).Caption = data.GetTelegramFormatedText(isSpoiler);
                                    ((InputMedia)inputMedia).ParseMode = ParseMode.Html;
                                }
                                albumMedia.Add(inputMedia);
                            }

                            _ = await botClient.SendMediaGroupAsync(chatId: message.Chat, messageThreadId: messageThreadId, media: albumMedia, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            logger.LogError("No medias found in {URL}", uri.AbsoluteUri);
                        }
                        break;
                    case ScrapedDataType.Text:
                        _ = await botClient.SendTextMessageAsync(chatId: message.Chat, messageThreadId: messageThreadId, text: data.GetTelegramFormatedText(isSpoiler), parseMode: ParseMode.Html, cancellationToken: cancellationToken);
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
            logger.LogError(ex, "Failed for {Message} for chat {chatName}, {threadId}", message.Text, message.Chat.Title + message.Chat.Username, message.MessageThreadId);
        }
    }
}