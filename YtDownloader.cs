using CliWrap;
using CliWrap.Buffered;
using System.Text.RegularExpressions;

namespace TelegramMediaGrabberBot
{
    public static class YtDownloader
    {
        public static async Task<Stream?> DownloadVideoFromUrlAsync(string url)
        {
            var fileName = Regex.Replace(url, @"[^0-9a-zA-Z\._]", "");
            if (fileName.Length > 100)
            {
                fileName = fileName[..100];
            }

            fileName = $"tmp/{fileName}";
            _ = await Cli.Wrap("yt-dlp")
                .WithArguments(new[] {
                    "-o", fileName
                    , url })
                //.WithWorkingDirectory("/usr/local/bin")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();


            if (File.Exists(fileName))
            {
                var bytes = await File.ReadAllBytesAsync(fileName);
                Stream stream = new MemoryStream(bytes);
                return stream;
            }

            return null;
        }
    }
}
