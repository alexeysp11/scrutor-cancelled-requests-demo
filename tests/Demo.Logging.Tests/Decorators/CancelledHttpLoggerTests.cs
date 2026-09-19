using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Demo.Logging.Decorators;
using Demo.Logging.Tests.Decorators.Fakes;

namespace Demo.Logging.Tests.Decorators;

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
    public void Suppresses_OperationCanceledException_WhenRequestWasAborted()
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
    public void LogsNormalErrors_EvenWhenRequestWasAborted()
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
    public void LogsOperationCanceledException_WhenRequestWasNotAborted()
    {
        var (sink, decorator, accessor) = CreateSut();

        accessor.HttpContext = new DefaultHttpContext();

        var logger = decorator.CreateLogger("TestCategory");
        logger.LogError(new OperationCanceledException(), "request failed");

        Assert.Single(sink.Entries);
    }

    [Fact]
    public void DoesNotThrow_WhenHttpContextIsNull_eg_background_job()
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
    public void DecoratingILoggerProvider_WrapsEveryRegisteredProvider()
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
