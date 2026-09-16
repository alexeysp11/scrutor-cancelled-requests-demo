using Microsoft.Extensions.Logging;

namespace Demo.Scenarios;

public sealed record PaymentRequest(string OrderId, decimal Amount);

public sealed record PaymentResult(bool Success, string? TransactionId);

public interface IPaymentGatewayClient
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct);
}

/// <summary>
/// Retry decorator around <see cref="IPaymentGatewayClient"/>. Register with Scrutor:
/// <code>services.Decorate&lt;IPaymentGatewayClient, RetryingPaymentGatewayClient&gt;();</code>
/// </summary>
public sealed class RetryingPaymentGatewayClient(
    IPaymentGatewayClient inner,
    ILogger<RetryingPaymentGatewayClient> logger) : IPaymentGatewayClient
{
    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await inner.ChargeAsync(request, ct);
            }
            catch (HttpRequestException ex) when (attempt++ < 3)
            {
                logger.LogWarning(ex, "Payment gateway call failed, retrying (attempt {Attempt})", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct);
            }
        }
    }
}
