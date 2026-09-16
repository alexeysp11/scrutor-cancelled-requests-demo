using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Demo.Logging;

namespace Demo.Tests;

public class CancelledHttpLoggerTests
{
    private static (RecordingLoggerProvider sink, ILoggerProvider decorator, IHttpContextAccessor accessor) CreateSut()
    {
        var services = new ServiceCollection();
        var sink = new RecordingLoggerProvider();

        services.AddSingleton<ILoggerProvider>(sink);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();

        var provider = services.BuildServiceProvider();
        var decorator = provider.GetRequiredService<ILoggerProvider>();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();

        return (sink, decorator, accessor);
    }

    [Fact]
    public void Suppresses_OperationCanceledException_when_request_was_aborted()
    {
        var (sink, decorator, accessor) = CreateSut();

        var httpContext = new DefaultHttpContext();
        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;
        cts.Cancel();
        accessor.HttpContext = httpContext;

        var logger = decorator.CreateLogger("TestCategory");
        logger.LogError(new OperationCanceledException(), "request failed");

        Assert.Empty(sink.Entries);
    }

    [Fact]
    public void Logs_normal_errors_even_when_request_was_aborted()
    {
        var (sink, decorator, accessor) = CreateSut();

        var httpContext = new DefaultHttpContext();
        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;
        cts.Cancel();
        accessor.HttpContext = httpContext;

        var logger = decorator.CreateLogger("TestCategory");
        logger.LogError(new InvalidOperationException("boom"), "request failed");

        var entry = Assert.Single(sink.Entries);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    public void Logs_OperationCanceledException_when_request_was_not_aborted()
    {
        var (sink, decorator, accessor) = CreateSut();

        accessor.HttpContext = new DefaultHttpContext();

        var logger = decorator.CreateLogger("TestCategory");
        logger.LogError(new OperationCanceledException(), "request failed");

        Assert.Single(sink.Entries);
    }

    [Fact]
    public void Does_not_throw_when_HttpContext_is_null_eg_background_job()
    {
        var (sink, decorator, accessor) = CreateSut();

        accessor.HttpContext = null;

        var logger = decorator.CreateLogger("BackgroundJob");
        var exception = Record.Exception(() =>
            logger.LogError(new OperationCanceledException(), "cancelled somewhere in the background"));

        Assert.Null(exception);
        Assert.Single(sink.Entries);
    }

    [Fact]
    public void Decorating_ILoggerProvider_wraps_every_registered_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerProvider>(new RecordingLoggerProvider());
        services.AddSingleton<ILoggerProvider>(new RecordingLoggerProvider());
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetServices<ILoggerProvider>().ToList();

        Assert.Equal(2, resolved.Count);
        Assert.All(resolved, p => Assert.IsType<CancelledHttpLoggerProvider>(p));
    }
}
