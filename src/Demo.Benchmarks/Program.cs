using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Demo.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

BenchmarkRunner.Run<CancelledHttpLoggerBenchmarks>();

[MemoryDiagnoser]
public class CancelledHttpLoggerBenchmarks
{
    private static readonly InvalidOperationException RealError = new("boom");
    private static readonly OperationCanceledException CancelledError = new();

    private ILogger _plainLogger = null!;
    private ILogger _decoratedLogger = null!;
    private IHttpContextAccessor _abortedAccessor = null!;

    [GlobalSetup]
    public void Setup()
    {
        var sink = new NoOpLoggerProvider();
        _plainLogger = sink.CreateLogger("Benchmark");

        var httpContext = new DefaultHttpContext();
        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;
        cts.Cancel();

        _abortedAccessor = new StaticHttpContextAccessor(httpContext);

        var decoratedProvider = new CancelledHttpLoggerProvider(sink, _abortedAccessor);
        _decoratedLogger = decoratedProvider.CreateLogger("Benchmark");
    }

    [Benchmark(Baseline = true)]
    public void NoDecorator_NormalError() =>
        _plainLogger.LogError(RealError, "request failed");

    [Benchmark]
    public void Decorated_NormalError_PassesThrough() =>
        _decoratedLogger.LogError(RealError, "request failed");

    [Benchmark]
    public void Decorated_CancelledError_Suppressed() =>
        _decoratedLogger.LogError(CancelledError, "request failed");
}

file sealed class NoOpLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new NoOpLogger();
    public void Dispose() { }
}

file sealed class NoOpLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _ = formatter(state, exception);
}

file sealed class StaticHttpContextAccessor(HttpContext context) : IHttpContextAccessor
{
    public HttpContext? HttpContext { get => context; set => throw new NotSupportedException(); }
}
