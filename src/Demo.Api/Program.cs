using Demo.Logging.Extensions;

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

// Use a long-running operation to simulate a request that can be aborted by the client.
app.MapGet("/slow", async (HttpContext context, ILogger<Program> logger, CancellationToken ct) =>
{
    logger.LogInformation("Started a long operation for {Path}", context.Request.Path);

    // If the client cancels the request, Task.Delay throws OperationCanceledException.
    // The exception bubbles up to ASP.NET Core infrastructure, where our decorator suppresses the log.
    await Task.Delay(TimeSpan.FromSeconds(3), ct);

    logger.LogInformation("Finished a long operation for {Path}", context.Request.Path);
    return Results.Ok("Done");
})
.WithName("Slow");

app.Run();
