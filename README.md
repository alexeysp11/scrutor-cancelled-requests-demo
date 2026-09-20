# HTTP Request Cancellation Log Suppression via Scrutor

[English](README.md) | [Русский](README.ru.md)

Demonstration of suppressing `OperationCanceledException` logs caused by client disconnections in ASP.NET Core applications using an `ILoggerProvider` decorator configured via [Scrutor](https://github.com).

---

## Background

### Problem
In ASP.NET Core web applications, when a client prematurely aborts an HTTP request (e.g., closes a browser tab or cancels a `curl` invocation), underlying asynchronous operations throw an `OperationCanceledException`. By default, this exception propagates up to the Kestrel infrastructure or standard middleware, flooding the application console and log storage with noisy, low-value `ERROR` level stack traces.

### Solution
Instead of scattering boilerplate `try-catch` blocks across every endpoint or controller, this repository demonstrates a centralized approach:
1. Register `IHttpContextAccessor` to track active web requests.
2. Intercept and decorate all registered `ILoggerProvider` instances using **Scrutor**.
3. Inspect incoming exceptions inside the custom logger: if an `OperationCanceledException` correlates with `HttpContext.RequestAborted.IsCancellationRequested`, the log entry is silently dropped. All other logs pass through untouched.

The code for the decorator provider and the logger that filters out unnecessary entries might look something like this:
```csharp
public sealed class CancelledHttpLoggerProvider(
    ILoggerProvider inner,
    IHttpContextAccessor accessor) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new CancelledHttpLogger(inner.CreateLogger(categoryName), accessor);

    public void Dispose() => inner.Dispose();
}

public sealed class CancelledHttpLogger(
    ILogger inner,
    IHttpContextAccessor accessor) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (exception is OperationCanceledException &&
            accessor.HttpContext?.RequestAborted.IsCancellationRequested == true)
        {
            return;
        }

        inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
```

and registration:
```csharp
services.AddHttpContextAccessor();
services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();
```

---

## Project Structure

- `src/Demo.Logging` — Core logic (`CancelledHttpLoggerProvider` and `CancelledHttpLogger`).
- `src/Demo.Api` — Minimal ASP.NET Core API with decorator registration.
- `src/Demo.Benchmarks` — BenchmarkDotNet suite comparing raw and decorated logging performance.
- `tests/Demo.Tests` — Unit and integration tests covering edge cases.

---

## Running the Application

```bash
dotnet run --project src/Demo.Api
```
The server listens on HTTP (port `5241`) and HTTPS (port `7040`).

---

### Verifying via curl

Test the `GET /slow` endpoint (simulates a 3-second delay) to check the log suppression behavior:

#### HTTPS Verification (Handles redirection)
* **Successful execution:**
  ```bash
  curl -k -L --max-time 5 https://localhost:7040/slow
  ```
  *Console output:* Prints both `"Started..."` and `"Finished..."` logs.

* **Aborted request (Suppression test):**
  ```bash
  curl -k -L --max-time 1 https://localhost:7040/slow
  ```
  *Console output:* Prints **only** the `"Started..."` log. The standard Kestrel/ASP.NET Core `OperationCanceledException` error stack trace is suppressed.

#### HTTP Verification (Bypasses endpoint via 307 redirect)
* **Aborted early:** `curl --max-time 1 http://localhost:5241/slow` (Terminates at the redirection middleware; no suppression occurs).
* **Full execution:** `curl --max-time 5 http://localhost:5241/slow` (Returns a `307 Temporary Redirect`).

---

## Running Benchmarks

```bash
dotnet run -c Release --project src/Demo.Benchmarks
```
Focus on the relative execution speed and zero-allocation profiles rather than absolute nanosecond values.
