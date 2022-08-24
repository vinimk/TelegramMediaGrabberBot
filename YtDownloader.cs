using CliWrap;
using CliWrap.Buffered;
using System.Text;

namespace TelegramMediaGrabberBot
{
    public static class YtDownloader
    {
        private static DateTime LastUpdateOfYtDlp;
        private static readonly ILogger log = ApplicationLogging.CreateLogger("YtDownloader");
        static YtDownloader() => LastUpdateOfYtDlp = new();
        public static async Task<Stream?> DownloadVideoFromUrlAsync(string url, bool updatedYtDl = false)
        {
            var fileName = $"tmp/{Guid.NewGuid()}";

            StringBuilder stdOutBuffer = new();
            StringBuilder stdErrBuffer = new();

            _ = await Cli.Wrap("yt-dlp")
                .WithArguments(new[] {
                    "-o", fileName
                    , url })
                //.WithWorkingDirectory("/usr/local/bin")
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteBufferedAsync();

            if (stdOutBuffer.Length > 0)
            {
                log.LogInformation(stdOutBuffer.ToString());
            }

            if (File.Exists(fileName))
            {
                var bytes = await File.ReadAllBytesAsync(fileName);
                Stream stream = new MemoryStream(bytes);
                return stream;
            }
            else
            {
                if (stdErrBuffer.Length > 0)
                {
                    log.LogError(stdErrBuffer.ToString());

                    if (DateTime.Compare(LastUpdateOfYtDlp, DateTime.Now.AddDays(-1)) < 0) //update only once a day
                    {
                        LastUpdateOfYtDlp = DateTime.Now;
                        StringBuilder stdOutBufferUpdate = new();

                        log.LogInformation("Updating yt-dlp");

                        _ = await Cli.Wrap("yt-dlp")
                            .WithArguments(new[] {
                            "-U"
                            })
                        .WithValidation(CommandResultValidation.None)
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBufferUpdate))
                        .ExecuteBufferedAsync();

                        log.LogInformation(stdOutBufferUpdate.ToString());

                        if (!updatedYtDl)
                            return await DownloadVideoFromUrlAsync(url, true);
                    }
                }
            }

            return null;
        }
    }
}
