using Demo.Logging.Decorators;
using Demo.Logging.Extensions;
using Demo.Logging.Tests.Decorators.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demo.Logging.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCancelledRequestLogSuppression_ShouldRegisterRequiredServicesAndDecorateLoggerProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register a dummy provider so Scrutor has something to decorate
        services.AddSingleton<ILoggerProvider, RecordingLoggerProvider>();

        // Act
        services.AddCancelledRequestLogSuppression();
        var provider = services.BuildServiceProvider();

        // Assert
        // 1. Verify HttpContextAccessor is registered
        var accessor = provider.GetService<IHttpContextAccessor>();
        Assert.NotNull(accessor);

        // 2. Verify ILoggerProvider is decorated with CancelledHttpLoggerProvider
        var loggerProvider = provider.GetService<ILoggerProvider>();
        Assert.NotNull(loggerProvider);
        Assert.IsType<CancelledHttpLoggerProvider>(loggerProvider);
    }
}
