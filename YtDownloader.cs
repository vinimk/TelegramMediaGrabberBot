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
        var urlResult = await Cli.Wrap("yt-dlp")
                                .WithArguments(new[] {
                                            "--get-url"
                                            , url })
                                .WithValidation(CommandResultValidation.None)
                                .ExecuteBufferedAsync();
        if (urlResult.StandardOutput.Length > 0 &&
            urlResult.StandardOutput.Split("\n").Length <= 2) //workarround for some providers (youtube shorts for ex) that has different tracks for video/sound
        {
            return new Video { contentUri = new Uri(urlResult.StandardOutput) };
        }

        string fileName = $"tmp/{Guid.NewGuid()}.mp4";
        var dlResult = await Cli.Wrap("yt-dlp")
                                .WithArguments(new[] {
                                            //"-vU"
                                            "-o", fileName
                                            ,"--add-header"
                                            ,"User-Agent:facebookexternalhit/1.1"
                                            ,"--embed-metadata"
                                            ,"--exec" ,"echo"
                                            , url })
                                .WithValidation(CommandResultValidation.None)
                                .ExecuteBufferedAsync();

        if (dlResult.StandardOutput.Length > 0)
        {
            log.LogInformation(dlResult.StandardOutput);

            var output = dlResult.StandardOutput.Split(Environment.NewLine);

            fileName = output[^2];
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
            if (dlResult.StandardError.Length > 0)
            {
                log.LogError(dlResult.StandardError.ToString());

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
