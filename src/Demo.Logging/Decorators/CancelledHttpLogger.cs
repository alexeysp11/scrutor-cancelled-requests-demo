using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Logging.Decorators;

/// <summary>
/// Decorates an <see cref="ILogger"/> and suppresses <see cref="OperationCanceledException"/>
/// entries that correspond to the current HTTP request being aborted by the client.
/// </summary>
public sealed class CancelledHttpLogger(
    ILogger inner,
    IHttpContextAccessor accessor) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        inner.BeginScope(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (exception is OperationCanceledException &&
            accessor.HttpContext?.RequestAborted.IsCancellationRequested == true)
        {
            return;
        }

        inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
