using Demo.Scenarios;

internal sealed class FakePaymentGatewayClient : IPaymentGatewayClient
{
    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct) =>
        Task.FromResult(new PaymentResult(true, Guid.NewGuid().ToString("N")));
}
