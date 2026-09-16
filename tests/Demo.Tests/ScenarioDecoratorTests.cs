using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Demo.Scenarios;

namespace Demo.Tests;

internal sealed class ThrowsOnceThenSucceedsPaymentClient : IPaymentGatewayClient
{
    private int _calls;

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        _calls++;
        if (_calls == 1)
        {
            throw new HttpRequestException("transient network error");
        }

        return Task.FromResult(new PaymentResult(true, "tx-1"));
    }
}

internal sealed class RecordingOrderRepository : IOrderRepository
{
    public List<Order> Saved { get; } = [];

    public Task SaveAsync(Order order, CancellationToken ct)
    {
        Saved.Add(order);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingAuditLog : IAuditLog
{
    public List<string> Messages { get; } = [];

    public Task RecordAsync(string message, CancellationToken ct)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}

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
