namespace Demo.Scenarios;

public interface IPaymentGatewayClient
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct);
}
