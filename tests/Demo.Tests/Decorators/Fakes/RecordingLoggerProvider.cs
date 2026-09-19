using Microsoft.Extensions.Logging;

namespace Demo.Tests.Decorators.Fakes;

/// <summary>
/// Test double that records every message it receives, without any real sink behind it.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    public List<(string Category, LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Entries);

    public void Dispose() { }

    private sealed class RecordingLogger(
        string categoryName,
        List<(string Category, LogLevel Level, string Message, Exception? Exception)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add((categoryName, logLevel, formatter(state, exception), exception));
        }
    }
}
