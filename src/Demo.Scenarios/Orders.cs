using Microsoft.AspNetCore.Http;

namespace Demo.Scenarios;

public sealed record Order(string Id, string ProductName);

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken ct);
}

public interface IAuditLog
{
    Task RecordAsync(string message, CancellationToken ct);
}

/// <summary>
/// Audit decorator around <see cref="IOrderRepository"/> that enriches every save with the
/// current user, taken from <see cref="IHttpContextAccessor"/>. Register with Scrutor:
/// <code>services.Decorate&lt;IOrderRepository, AuditingOrderRepository&gt;();</code>
/// </summary>
public sealed class AuditingOrderRepository(
    IOrderRepository inner,
    IHttpContextAccessor accessor,
    IAuditLog auditLog) : IOrderRepository
{
    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        await inner.SaveAsync(order, ct);

        var userId = accessor.HttpContext?.User.FindFirst("sub")?.Value ?? "system";
        await auditLog.RecordAsync($"Order {order.Id} saved by {userId}", ct);
    }
}
