using Microsoft.AspNetCore.Http;

namespace Demo.FailedAttempts;

/// <summary>
/// FAILED ATTEMPT #2 — see README.md in this project.
///
/// Swallows an <see cref="OperationCanceledException"/> that reaches the top of the
/// pipeline unhandled. This works only when nothing further down the call stack already
/// caught and logged the exception itself — see
/// <see cref="OrderRepositoryWithOwnLogging"/> for the case where it doesn't.
/// </summary>
public sealed class SuppressCancelledRequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Запрос отменён клиентом на нашей стороне — это не ошибка, не логируем.
        }
    }
}
