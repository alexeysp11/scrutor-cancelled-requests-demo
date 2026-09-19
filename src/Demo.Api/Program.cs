using Demo.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Decorator: suppress OperationCanceledException caused by HTTP request cancellation.
builder.Services.AddCancelledRequestLogSuppression();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// A long-running endpoint that can be cancelled (Ctrl+C in curl / closing the browser tab)
// to see log suppression in action.
app.MapGet("/slow", async (HttpContext context, ILogger<Program> logger, CancellationToken ct) =>
{
    logger.LogInformation("Started a long operation for {Path}", context.Request.Path);
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
        return Results.Ok("Done");
    }
    catch (OperationCanceledException ex)
    {
        // If the request is cancelled, this exception will propagate to both our code
        // and the ILoggerProvider below. CancelledHttpLoggerProvider will suppress
        // logging it if the exception resulted from the cancellation of the current HTTP request.
        logger.LogError(ex, "A long-running operation for {Path} was cancelled or failed", context.Request.Path);
        throw;
    }
})
.WithName("Slow");

app.Run();
