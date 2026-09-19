# HTTP Request Cancellation Log Suppression via Scrutor

Demonstration of suppressing `OperationCanceledException` logs caused by client disconnections in ASP.NET Core applications using an `ILoggerProvider` decorator configured via [Scrutor](https://github.com).

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
