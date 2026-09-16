using Microsoft.Extensions.Logging;

namespace Demo.FailedAttempts;

public sealed record Order(int Id);

public interface IOrderStore
{
    Task<Order> GetOrderAsync(int id, CancellationToken ct);
}

/// <summary>
/// Demonstrates why <see cref="SuppressCancelledRequestLoggingMiddleware"/> alone is not
/// enough: this repository logs the exception itself, before it ever bubbles up to the
/// middleware. When <paramref name="ct"/> is cancelled mid-query, the resulting
/// <see cref="OperationCanceledException"/> lands in the generic <c>catch (Exception ex)</c>
/// below and is logged as a real error — the middleware's try/catch never gets a chance to
/// matter, because the log entry has already been written.
/// </summary>
public sealed class OrderRepositoryWithOwnLogging(
    FakeDbContext db,
    ILogger<OrderRepositoryWithOwnLogging> logger) : IOrderStore
{
    public async Task<Order> GetOrderAsync(int id, CancellationToken ct)
    {
        try
        {
            return await db.SingleOrderAsync(id, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load order {OrderId}", id);
            throw;
        }
    }
}

/// <summary>
/// Minimal stand-in for something like EF Core's <c>DbContext</c> — just enough to
/// demonstrate that a cancelled query throws <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class FakeDbContext
{
    public async Task<Order> SingleOrderAsync(int id, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        return new Order(id);
    }
}
