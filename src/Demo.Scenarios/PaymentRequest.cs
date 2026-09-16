namespace Demo.Scenarios;

public sealed record PaymentRequest(string OrderId, decimal Amount);
