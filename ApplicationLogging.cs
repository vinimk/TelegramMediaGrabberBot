using CommunityToolkit.Diagnostics;

namespace TelegramMediaGrabberBot;

internal static class ApplicationLogging
{
    internal static ILoggerFactory? LoggerFactory { get; set; }// = new LoggerFactory();
    internal static ILogger CreateLogger<T>()
    {
        Guard.IsNotNull(LoggerFactory);
        return LoggerFactory.CreateLogger<T>();
    }

    internal static ILogger CreateLogger(string categoryName)
    {
        Guard.IsNotNull(LoggerFactory);
        return LoggerFactory.CreateLogger(categoryName);
    }
}
