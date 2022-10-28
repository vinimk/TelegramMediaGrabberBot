namespace TelegramMediaGrabberBot;

internal static class ApplicationLogging
{
    internal static ILoggerFactory? LoggerFactory { get; set; }// = new LoggerFactory();
    internal static ILogger CreateLogger<T>()
    {
        ArgumentNullException.ThrowIfNull(LoggerFactory);
        return LoggerFactory.CreateLogger<T>();
    }

    internal static ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(LoggerFactory);
        return LoggerFactory.CreateLogger(categoryName);
    }
}
