using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Logging.Decorators;

/// <summary>
/// Decorates an <see cref="ILoggerProvider"/> so every <see cref="ILogger"/> it creates
/// is wrapped in a <see cref="CancelledHttpLogger"/>. Register with Scrutor:
/// <code>services.Decorate&lt;ILoggerProvider, CancelledHttpLoggerProvider&gt;();</code>
/// </summary>
public sealed class CancelledHttpLoggerProvider(
    ILoggerProvider inner,
    IHttpContextAccessor accessor) : ILoggerProvider
{
    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) =>
        new CancelledHttpLogger(inner.CreateLogger(categoryName), accessor);

    /// <inheritdoc/>
    public void Dispose() => inner.Dispose();
}
