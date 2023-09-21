using CliWrap;
using CliWrap.Buffered;
using System.Text;
using TelegramMediaGrabberBot.DataStructures.Medias;

namespace TelegramMediaGrabberBot.Utils;

public static class YtDownloader
{
    private static DateTime LastUpdateOfYtDlp;
    private static readonly ILogger log = ApplicationLogging.CreateLogger("YtDownloader");
    //private static string InstagramUserName = "";
    //private static string InstagramPassword = "";
    static YtDownloader()
    {
        LastUpdateOfYtDlp = new();
    }

    public static async Task<MediaDetails?> DownloadVideoFromUrlAsync(string url, bool forceDownload = false, bool updatedYtDl = false)
    {
        if (forceDownload == false) //only use the getURL method if ForceDownload is disabled
        {
            BufferedCommandResult urlResult = await Cli.Wrap("yt-dlp")
                                    .WithArguments(new[]
                                        {
                                            "--get-url", url
                                            //,"--username", InstagramUserName
                                            //, "--password", InstagramPassword
                                        }
                                    ).WithValidation(CommandResultValidation.None)
                                    .ExecuteBufferedAsync();
            if (urlResult.StandardOutput.Length > 0 &&
                urlResult.StandardOutput.Split("\n").Length <= 2) //workarround for some providers (youtube shorts for ex) that has different tracks for video/sound
            {
                return new MediaDetails { Uri = new Uri(urlResult.StandardOutput.Replace("\n", "")), Type = MediaType.Video };
            }
        }

        string fileName = $"tmp/{Guid.NewGuid()}.mp4";
        BufferedCommandResult dlResult = await Cli.Wrap("yt-dlp")
                                .WithArguments(new[]
                                {
                                    //"-vU"
                                    "-o", fileName
                                    ,"--add-header","User-Agent:facebookexternalhit/1.1"
                                    ,"--embed-metadata"
                                    ,"--exec" ,"echo"
                                    //,"--username", InstagramUserName
                                    //, "--password", InstagramPassword
                                    , url
                                }
                                ).WithValidation(CommandResultValidation.None)
                                .ExecuteBufferedAsync();

        if (dlResult.StandardOutput.Length > 0) //workarround for some providers (ie tiktok that return weird separated filenames)
        {
            log.LogInformation("Info: {buffer}", dlResult.StandardOutput);

            string[] output = dlResult.StandardOutput.Split(Environment.NewLine);

            fileName = output[^2];
        }

        if (File.Exists(fileName))
        {
            byte[] bytes = await File.ReadAllBytesAsync(fileName);
            Stream stream = new MemoryStream(bytes);
            log.LogInformation("downloaded video for url {url} size: {size}MB", url, stream.Length / 1024.0f / 1024.0f);

            try
            {
                FileInfo fi = new(fileName);

                TagLib.File tfile = TagLib.File.Create(fileName, $"video/{fi.Extension.Replace(".", string.Empty)}", TagLib.ReadStyle.Average);

                string? description = tfile.Tag.Description;
                string? title = tfile.Tag.Title;
                string? author = tfile.Tag.Performers?.FirstOrDefault();
                return new MediaDetails()
                {
                    Stream = stream,
                    Content = description,
                    Author = author,
                    Type = MediaType.Video
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "metadata extraction for {url}", url);
                return new MediaDetails()
                {
                    Stream = stream,
                    Type = MediaType.Video
                };
            }

        }
        else
        {
            if (dlResult.StandardError.Length > 0) // if there is an error try and update yt-dl
            {
                log.LogError("Error downloading: {buffer}", dlResult.StandardError.ToString());

                if (DateTime.Compare(LastUpdateOfYtDlp, DateTime.Now.AddDays(-1)) < 0) //update only once a day
                {
                    LastUpdateOfYtDlp = DateTime.Now;


                    await UpdateYtDlpAsync();

                    if (!updatedYtDl)
                    {
                        return await DownloadVideoFromUrlAsync(url, forceDownload, true);
                    }
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

        log.LogInformation("Info: {buffer}", stdOutBufferUpdate.ToString());
    }
}