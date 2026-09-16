using Demo.Scenarios;

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    public Task SaveAsync(Order order, CancellationToken ct) => Task.CompletedTask;
}
