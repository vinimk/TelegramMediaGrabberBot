using CliWrap;
using CliWrap.Buffered;
using System.Text;
using TelegramMediaGrabberBot.DataStructures;

namespace TelegramMediaGrabberBot;

public static class YtDownloader
{
    private static DateTime LastUpdateOfYtDlp;
    private static readonly ILogger log = ApplicationLogging.CreateLogger("YtDownloader");
    static YtDownloader() => LastUpdateOfYtDlp = new();
    public static async Task<Video?> DownloadVideoFromUrlAsync(string url, bool updatedYtDl = false)
    {
        string fileName = $"tmp/{Guid.NewGuid()}.mp4";

        StringBuilder stdOutBuffer = new();
        StringBuilder stdErrBuffer = new();

        _ = await Cli.Wrap("yt-dlp")
            .WithArguments(new[] {
                //"-vU"
                "-o", fileName
                ,"--add-header"
                ,"User-Agent:facebookexternalhit/1.1"
                ,"--embed-metadata"
                ,"--exec" ,"echo"
                , url })
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
            .ExecuteBufferedAsync();

        if (stdOutBuffer.Length > 0)
        {
            log.LogInformation(stdOutBuffer.ToString());

            var output = stdOutBuffer.ToString().Split(Environment.NewLine);

            fileName = output[^2];

            stdOutBuffer.Clear();
        }

        if (File.Exists(fileName))
        {
            var bytes = await File.ReadAllBytesAsync(fileName);
            Stream stream = new MemoryStream(bytes);
            log.LogInformation("downloaded video for url {url} size: {size}MB", url, stream.Length / 1024.0f / 1024.0f);

            try
            {
                FileInfo fi = new(fileName);

                var tfile = TagLib.File.Create(fileName, $"video/{fi.Extension.Replace(".", String.Empty)}", TagLib.ReadStyle.Average);

                string? description = tfile.Tag.Description;
                string? title = tfile.Tag.Title;
                string? author = tfile.Tag.Performers?.FirstOrDefault();
                return new Video()
                {
                    Stream = stream,
                    Content = title + " " + description,
                    Author = author
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "metadata extraction");
                return new Video()
                {
                    Stream = stream
                };
            }

        }
        else
        {
            if (stdErrBuffer.Length > 0)
            {
                log.LogError(stdErrBuffer.ToString());

                if (DateTime.Compare(LastUpdateOfYtDlp, DateTime.Now.AddDays(-1)) < 0) //update only once a day
                {
                    LastUpdateOfYtDlp = DateTime.Now;


                    await UpdateYtDlpAsync();

                    if (!updatedYtDl)
                        return await DownloadVideoFromUrlAsync(url, true);
                }
            }
        }

        return null;
    }

    public static async Task UpdateYtDlpAsync()
    {
        log.LogInformation("Updating yt-dlp");

        StringBuilder stdOutBufferUpdate = new();

        _ = await Cli.Wrap("yt-dlp")
            .WithArguments(new[] {
                        "-U"
            })
        .WithValidation(CommandResultValidation.None)
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBufferUpdate))
        .ExecuteBufferedAsync();

        log.LogInformation(stdOutBufferUpdate.ToString());
    }
}
