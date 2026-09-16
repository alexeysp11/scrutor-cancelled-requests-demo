using Demo.Logging;
using Demo.Scenarios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Декоратор из статьи: подавляем OperationCanceledException, вызванный отменой HTTP-запроса.
builder.Services.AddHttpContextAccessor();
builder.Services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();

// "Аналогичные сценарии" из статьи, зарегистрированы для демонстрации того же приёма.
builder.Services.AddHttpClient<IPaymentGatewayClient, FakePaymentGatewayClient>();
builder.Services.Decorate<IPaymentGatewayClient, RetryingPaymentGatewayClient>();

builder.Services.AddSingleton<IAuditLog, ConsoleAuditLog>();
builder.Services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
builder.Services.Decorate<IOrderRepository, AuditingOrderRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Долгий эндпоинт, который можно отменить (Ctrl+C в curl / закрыть вкладку браузера),
// чтобы увидеть подавление лога вживую вместо "-- (ничего не выведено) --" из статьи.
app.MapGet("/slow", async (HttpContext context, ILogger<Program> logger, CancellationToken ct) =>
{
    logger.LogInformation("Начали долгую операцию для {Path}", context.Request.Path);
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
        return Results.Ok("Готово");
    }
    catch (OperationCanceledException ex)
    {
        // Если запрос отменили — это исключение долетит и до нашего кода, и до
        // ILoggerProvider ниже. CancelledHttpLoggerProvider подавит его запись в лог,
        // если это была именно отмена текущего HTTP-запроса.
        logger.LogError(ex, "Долгая операция для {Path} была отменена или упала", context.Request.Path);
        throw;
    }
})
.WithName("Slow");

app.MapPost("/orders/{id}", async (string id, IOrderRepository repository, CancellationToken ct) =>
{
    await repository.SaveAsync(new Order(id, "Demo product"), ct);
    return Results.Ok();
})
.WithName("SaveOrder");

app.Run();

internal sealed class FakePaymentGatewayClient : IPaymentGatewayClient
{
    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct) =>
        Task.FromResult(new PaymentResult(true, Guid.NewGuid().ToString("N")));
}

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    public Task SaveAsync(Order order, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class ConsoleAuditLog(ILogger<ConsoleAuditLog> logger) : IAuditLog
{
    public Task RecordAsync(string message, CancellationToken ct)
    {
        logger.LogInformation("[AUDIT] {Message}", message);
        return Task.CompletedTask;
    }
}
