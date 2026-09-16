using Demo.Scenarios;

namespace Demo.Tests;

internal sealed class RecordingAuditLog : IAuditLog
{
    public List<string> Messages { get; } = [];

    public Task RecordAsync(string message, CancellationToken ct)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
