using CliWrap;
using CliWrap.Buffered;
using System.Text;

namespace TelegramMediaGrabberBot
{
    public static class YtDownloader
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("YtDownloader");

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
                    stdOutBuffer.Clear();
                    log.LogError(stdErrBuffer.ToString());

                    _ = await Cli.Wrap("yt-dlp")
                    .WithValidation(CommandResultValidation.None)
                    .WithArguments(new[] {
                    "-U" })
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .ExecuteBufferedAsync();

                    log.LogInformation(stdOutBuffer.ToString());

                    if (!updatedYtDl)
                        return await DownloadVideoFromUrlAsync(url, true);
                }
            }

            return null;
        }
    }
}
