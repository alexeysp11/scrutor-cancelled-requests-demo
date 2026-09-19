using Demo.Logging.Decorators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demo.Logging.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Suppress OperationCanceledException caused by HTTP request cancellation.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCancelledRequestLogSuppression(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();
        return services;
    }
}
