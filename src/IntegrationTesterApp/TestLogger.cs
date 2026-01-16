using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SKRoutingStyles.IntegrationTesterApp;

/// <summary>
/// Logger that captures function calls and displays emoji logs to console.
/// </summary>
public class TestLogger : ILogger
{
    private readonly string _categoryName;
    private readonly List<string> _functionCalls = new();
    private static readonly Regex FunctionCallPattern = new(@"🔧\s*\[FUNCTION CALL\]\s*(\w+\.\w+)", RegexOptions.Compiled);
    private static readonly Regex EmojiPattern = new(@"🔧|✅|💬|💡", RegexOptions.Compiled);

    public TestLogger(string categoryName)
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

        // Capture function calls for verification
        var match = FunctionCallPattern.Match(message);
        if (match.Success)
        {
            _functionCalls.Add(match.Groups[1].Value);
        }

        // Display emoji-prefixed messages to console
        if (EmojiPattern.IsMatch(message))
        {
            var emojiIndex = message.IndexOf("💬");
            if (emojiIndex == -1)
                emojiIndex = message.IndexOf("💡");
            if (emojiIndex == -1)
                emojiIndex = message.IndexOf("🔧");
            if (emojiIndex == -1)
                emojiIndex = message.IndexOf("✅");

            if (emojiIndex >= 0)
                Console.WriteLine(message.Substring(emojiIndex));
        }
    }

    public IReadOnlyList<string> GetFunctionCalls() => _functionCalls;
    public void Clear() => _functionCalls.Clear();
}

public class TestLoggerProvider : ILoggerProvider
{
    private readonly Dictionary<string, TestLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        if (!_loggers.TryGetValue(categoryName, out var logger))
        {
            logger = new TestLogger(categoryName);
            _loggers[categoryName] = logger;
        }
        return logger;
    }

    public TestLogger? GetTestLogger(string categoryName)
    {
        _loggers.TryGetValue(categoryName, out var logger);
        return logger;
    }

    public void Dispose() { }
}
