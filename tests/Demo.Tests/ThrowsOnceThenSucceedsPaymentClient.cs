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
