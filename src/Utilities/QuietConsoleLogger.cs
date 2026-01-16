using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SKRoutingStyles.Utilities;

/// <summary>
/// Console logger that only shows emoji-prefixed log messages.
/// </summary>
public class QuietConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private static readonly Regex EmojiPattern = new(@"🔧|✅", RegexOptions.Compiled);

    public QuietConsoleLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        // Only log messages containing emoji indicators
        if (EmojiPattern.IsMatch(message))
        {
            var emojiIndex = message.IndexOf("🔧");
            if (emojiIndex == -1)
                emojiIndex = message.IndexOf("✅");

            if (emojiIndex >= 0)
                Console.WriteLine(message.Substring(emojiIndex));
        }
    }
}

public class QuietConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new QuietConsoleLogger(categoryName);
    public void Dispose() { }
}
