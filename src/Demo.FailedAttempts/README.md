# Unsuccessful Approaches

[English](README.md) | [Русский](README.ru.md)

This project does not participate in the final solution. It is included in the build solely so that both unsuccessful approaches can be inspected and run locally, rather than just seen as snippets in the article.

## Approach #1 — `nlog.config.attempt.xml`

An NLog filter based on the exception type (`contains('${exception:format=Type}', 'OperationCanceledException')`).
This does not work because it filters strictly by the exception *type*, whereas we need to filter by the *root cause* (whether the request was explicitly aborted by the user). This context is entirely unavailable to the logger configuration.

## Approach #2 — `SuppressCancelledRequestLoggingMiddleware.cs`

A custom middleware designed to catch unhandled exceptions at the top of the HTTP pipeline. This only works if the exception propagates all the way up to the middleware unhandled. The `OrderRepositoryWithOwnLogging.cs` class demonstrates a scenario where this approach fails: the repository logs the exception internally before it can propagate upward, rendering the middleware's `try/catch` block completely useless.
