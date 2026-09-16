using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Demo.Scenarios;

namespace Demo.Tests;

public class ScenarioDecoratorTests
{
    [Fact]
    public async Task RetryingPaymentGatewayClient_retries_transient_failures()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentGatewayClient, ThrowsOnceThenSucceedsPaymentClient>();
        services.AddSingleton<ILogger<RetryingPaymentGatewayClient>>(NullLogger<RetryingPaymentGatewayClient>.Instance);
        services.Decorate<IPaymentGatewayClient, RetryingPaymentGatewayClient>();

        var client = services.BuildServiceProvider().GetRequiredService<IPaymentGatewayClient>();

        var result = await client.ChargeAsync(new PaymentRequest("order-1", 10m), CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AuditingOrderRepository_records_current_user_from_HttpContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOrderRepository, RecordingOrderRepository>();
        services.AddSingleton<IAuditLog, RecordingAuditLog>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.Decorate<IOrderRepository, AuditingOrderRepository>();

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = null; // например, вызов из фонового джоба

        var repository = provider.GetRequiredService<IOrderRepository>();
        await repository.SaveAsync(new Order("o-1", "Widget"), CancellationToken.None);

        var auditLog = (RecordingAuditLog)provider.GetRequiredService<IAuditLog>();
        Assert.Equal("Order o-1 saved by system", Assert.Single(auditLog.Messages));
    }
}
