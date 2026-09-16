# Scrutor: Suppressing Cancelled HTTP Request Logs — Code Showcase

[English](README.md) | [Русский](README.ru.md)

A working demo project for the article on suppressing noisy `OperationCanceledException` logs using an `ILoggerProvider` decorator registered via [Scrutor](https://github.com/khellang/Scrutor). This repository includes the production-ready solution, both unsuccessful architectural approaches, and two additional decoration scenarios.

## Project Structure

- `src/Demo.Logging` — The working solution: `CancelledHttpLoggerProvider` and `CancelledHttpLogger`.
- `src/Demo.FailedAttempts` — Both unsuccessful approaches: an `nlog.config` filter and a custom middleware that catches the exception too late (see the dedicated `README.md` inside).
- `src/Demo.Scenarios` — Two additional Scrutor decoration use cases: a retry wrapper around an HTTP client and an audit logging mechanism via `IHttpContextAccessor`.
- `src/Demo.Api` — A minimal ASP.NET Core API with all decorators wired up and registered.
- `src/Demo.Benchmarks` — `BenchmarkDotNet` suite: it compares raw `ILogger.Log(...)` invocation, a decorator passing the log through, and a decorator suppressing the log.
- `tests/Demo.Tests` — Unit and integration tests validating the behavior described in the article: log suppression, `NullReferenceException` prevention outside HTTP context, multi-provider decoration side-effects, retries, and auditing.

## Getting Started

```bash
dotnet test
dotnet run --project src/Demo.Api
dotnet run -c Release --project src/Demo.Benchmarks
```

The `GET /slow` endpoint simulates a long-running 10-second operation. If you abort the request early (by closing the browser tab or running `curl --max-time 1 http://localhost:<port>/slow`), no error log will appear in the console — the `CancelledHttpLoggerProvider` will successfully suppress it.

Benchmarks must be run in the `Release` configuration (`BenchmarkDotNet` will reject running in `Debug`). Please note that absolute numbers depend heavily on your hardware. When replicating, look for the performance ratios between scenarios and the zero-allocation behavior, rather than the exact nanoseconds mentioned in the article.
