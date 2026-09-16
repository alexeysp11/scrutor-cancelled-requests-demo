using Demo.Scenarios;

internal sealed class ConsoleAuditLog(ILogger<ConsoleAuditLog> logger) : IAuditLog
{
    public Task RecordAsync(string message, CancellationToken ct)
    {
        logger.LogInformation("[AUDIT] {Message}", message);
        return Task.CompletedTask;
    }
}
