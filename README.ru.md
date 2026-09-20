# Подавление логов отмены HTTP-запросов с помощью Scrutor

[English](README.md) | [Русский](README.ru.md)

Пример подавления логов `OperationCanceledException`, возникающих при обрыве соединения клиентом в ASP.NET Core. Решение реализовано через декоратор `ILoggerProvider` с использованием библиотеки [Scrutor](https://github.com).

---

## Бэкграунд

### Проблема
В веб-приложениях ASP.NET Core, когда клиент преждевременно обрывает HTTP-запрос (например, закрывает вкладку браузера или прерывает команду `curl`), выполняющиеся асинхронные операции выбрасывают исключение `OperationCanceledException`. По умолчанию это исключение долетает до инфраструктуры Kestrel или стандартных Middleware, забивая консоль приложения и хранилище логов тоннами шумных трассировок стека со статусом `ERROR`.

### Решение
Вместо того чтобы расставлять шаблонные блоки `try-catch` внутри каждого эндпоинта или контроллера, данный репозиторий демонстрирует централизованный подход:
1. Подключается `IHttpContextAccessor` для отслеживания состояния текущего веб-запроса.
2. Все зарегистрированные экземпляры `ILoggerProvider` динамически декорируются с помощью библиотеки **Scrutor**.
3. Кастомный логгер анализирует поступающие исключения: если `OperationCanceledException` совпадает с признаком отмены запроса `HttpContext.RequestAborted.IsCancellationRequested`, запись лога блокируется. Все остальные логи проходят без изменений.

Код провайдера-декоратора и логгера, отсекающего лишние записи, может выглядеть примерно следующим образом:
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

и регистрация:
```csharp
services.AddHttpContextAccessor();
services.Decorate<ILoggerProvider, CancelledHttpLoggerProvider>();
```

---

## Структура проекта

- `src/Demo.Logging` — Компоненты решения (`CancelledHttpLoggerProvider` и `CancelledHttpLogger`).
- `src/Demo.Api` — Минимальный API на ASP.NET Core с регистрацией декоратора.
- `src/Demo.Benchmarks` — Бенчмарки BenchmarkDotNet для сравнения производительности прямого и декорированного логирования.
- `tests/Demo.Tests` — Юнит- и интеграционные тесты.

---

## Запуск приложения

```bash
dotnet run --project src/Demo.Api
```
Сервер слушает HTTP (порт `5241`) и HTTPS (порт `7040`).

---

### Проверка через curl

Для проверки поведения используется эндпоинт `GET /slow` (эмулирует задержку в 3 секунды):

#### Тестирование через HTTPS
* **Успешный запрос:**
  ```bash
  curl -k -L --max-time 5 https://localhost:7040/slow
  ```
  *Логи приложения:* Появятся обе записи `"Started..."` и `"Finished..."`.

* **Прерванный запрос (Проверка подавления):**
  ```bash
  curl -k -L --max-time 1 https://localhost:7040/slow
  ```
  *Логи приложения:* Выведется **только** запись `"Started..."`. Системный стек-трейс ошибки `OperationCanceledException` от Kestrel/ASP.NET Core будет подавлен.

#### Тестирование через HTTP
* **Прерывание на старте:** `curl --max-time 1 http://localhost:5241/slow` (Запрос завершается на этапе Middleware редиректа; подавление лога эндпоинта не происходит).
* **Выполнение до конца:** `curl --max-time 5 http://localhost:5241/slow` (Возвращает статус `307 Temporary Redirect`).

---

## Запуск бенчмарков

```bash
dotnet run -c Release --project src/Demo.Benchmarks
```
При анализе результатов оценивайте соотношение скорости сценариев и отсутствие аллокаций, а не конкретные наносекунды.
