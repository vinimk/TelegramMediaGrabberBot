﻿using CommunityToolkit.Diagnostics;
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
        _timer = new(TimeSpan.FromHours((double)appSettings.HoursBetweenBackgroundTask));

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken)
            && !stoppingToken.IsCancellationRequested)
        {
            DirectoryInfo di = new("tmp");
            IEnumerable<FileInfo> files = di.EnumerateFiles();
            _logger.LogInformation("Found {files} to delete", files.Count());
            foreach (FileInfo file in files)
            {
                try
                {
                    if (file.CreationTimeUtc < DateTime.UtcNow.AddMinutes(-5))  //not delete because if it recent could be in use
                    {
                        file.Delete();
                        _logger.LogInformation("Delted {file}", file.FullName);
                    }
                }
                catch
                {
                    _logger.LogError("Failed to delete {file}", file.FullName);
                }
            }
        }
    }
}