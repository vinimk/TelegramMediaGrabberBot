using CommunityToolkit.Diagnostics;
using TelegramMediaGrabberBot.Config;

namespace TelegramMediaGrabberBot.Services;

public class ClearTempBackgroundService : BackgroundService
{
    private readonly ILogger<ClearTempBackgroundService> _logger;

    private readonly PeriodicTimer _timer;

    public ClearTempBackgroundService(ILogger<ClearTempBackgroundService> logger, AppSettings appSettings)
    {
        Guard.IsNotNull(appSettings.HoursBetweenBackgroundTask);
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromHours((double)appSettings.HoursBetweenBackgroundTask));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken)
               && !stoppingToken.IsCancellationRequested)
            try
            {
                DirectoryInfo di = new("tmp");
                var files = di.EnumerateFiles();
                _logger.LogInformation("Found {files} to delete", files.Count());
                foreach (var file in files)
                    try
                    {
                        if (file.CreationTimeUtc <
                            DateTime.UtcNow.AddMinutes(-5)) //not delete because if it recent could be in use
                        {
                            file.Delete();
                            _logger.LogInformation("Delted {file}", file.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete {file}", file.FullName);
                    }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed");
            }
    }
}