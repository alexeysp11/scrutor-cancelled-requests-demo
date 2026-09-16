using Demo.Scenarios;

namespace Demo.Tests;

internal sealed class RecordingOrderRepository : IOrderRepository
{
    public List<Order> Saved { get; } = [];

    public Task SaveAsync(Order order, CancellationToken ct)
    {
        Saved.Add(order);
        return Task.CompletedTask;
    }
}
