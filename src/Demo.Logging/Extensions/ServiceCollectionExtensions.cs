using Demo.Logging.Decorators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demo.Logging.Extensions;

/// <summary>
/// Provides extension methods for setting up log suppression in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers infrastructure components to suppress noisy <see cref="OperationCanceledException"/> logs 
    /// caused by aborted HTTP requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining configuration calls.</returns>
    public static IServiceCollection AddCancelledRequestLogSuppression(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();
        return services;
    }
}
